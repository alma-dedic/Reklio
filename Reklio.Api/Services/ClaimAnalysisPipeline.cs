using System.Text.Json;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T4.2 + EPIC 9 — fiksni pipeline (NE agent loop): servisi se pozivaju unaprijed
// određenim redoslijedom, signali se agregiraju, deterministički gate donosi odluku,
// pa se odluka fiksira i TEK onda LLM objašnjava. Ovo je orkestrator koji worker zove.
public class ClaimAnalysisPipeline
{
    private const string ReceiptEvidenceType = "Receipt";
    private const string PhotoEvidenceType = "Photo";

    private readonly IOcrService _ocr;
    private readonly IVisionService _vision;
    private readonly IRagService _rag;
    private readonly IFraudService _fraud;
    private readonly IReceiptValidationService _validation;
    private readonly IDecisionGate _gate;
    private readonly IExplanationService _explanation;
    private readonly IClaimService _claims;
    private readonly IClaimEvidenceService _evidence;
    private readonly IPurchaseService _purchases;
    private readonly IProductService _products;
    private readonly INotificationService _notifications;
    private readonly ILogger<ClaimAnalysisPipeline> _logger;

    public ClaimAnalysisPipeline(
        IOcrService ocr,
        IVisionService vision,
        IRagService rag,
        IFraudService fraud,
        IReceiptValidationService validation,
        IDecisionGate gate,
        IExplanationService explanation,
        IClaimService claims,
        IClaimEvidenceService evidence,
        IPurchaseService purchases,
        IProductService products,
        INotificationService notifications,
        ILogger<ClaimAnalysisPipeline> logger)
    {
        _ocr = ocr;
        _vision = vision;
        _rag = rag;
        _fraud = fraud;
        _validation = validation;
        _gate = gate;
        _explanation = explanation;
        _claims = claims;
        _evidence = evidence;
        _purchases = purchases;
        _products = products;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<DecisionResult> RunAsync(int claimId, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetByIdAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        var evidence = await _evidence.GetByClaimAsync(claimId);
        var purchase = claim.PurchaseId is int linkedId
            ? await _purchases.GetByIdAsync(linkedId)
            : null;

        // 1. + 2. OCR i validacija dokaza kupovine.
        OcrResult? ocr = null;
        ReceiptValidationResult? validation = null;

        var receipt = evidence.FirstOrDefault(e => e.Type == ReceiptEvidenceType);
        if (receipt is not null)
        {
            // Fizička kupovina: slika je izvor istine — OCR pročita broj, validacija nađe
            // kupovinu, i tek tada vežemo reklamaciju za nju (razrješavanje).
            ocr = await _ocr.ExtractReceiptAsync(receipt.FilePath, cancellationToken);
            validation = await _validation.ValidateReceiptAsync(ocr);
            if (validation.PurchaseId is int resolvedId)
            {
                if (claim.PurchaseId != resolvedId)
                {
                    await _claims.LinkPurchaseAsync(claimId, resolvedId);
                }
                purchase = await _purchases.GetByIdAsync(resolvedId);
            }
        }
        else if (purchase is not null)
        {
            // Online kupovina: nema slike, lookup po broju kupovine (razriješena na submit-u).
            validation = await _validation.ValidateOnlineAsync(purchase.DocumentNumber);
        }

        var product = purchase is not null ? await _products.GetByIdAsync(purchase.ProductId) : null;

        // 3. Vizuelna analiza fotografija oštećenja.
        VisionResult? vision = null;
        var photos = evidence
            .Where(e => e.Type == PhotoEvidenceType)
            .Select(e => e.FilePath)
            .ToList();
        if (photos.Count > 0)
        {
            vision = await _vision.AnalyzeDamageAsync(photos, cancellationToken);
        }

        // 4. Provjera pravilnika (mek signal) — samo ako znamo kupovinu/kategoriju.
        RagResult? policy = null;
        if (purchase is not null)
        {
            var query = new RagQuery(
                Question: claim.IssueDescription,
                ProductCategory: product?.Category,
                IssueType: claim.IssueType);
            policy = await _rag.CheckPolicyAsync(query, cancellationToken);
        }

        // 5. Procjena rizika — samo ako je kupovina poznata (fn_features traži Purchase).
        FraudResult? fraud = null;
        if (purchase is not null)
        {
            fraud = await _fraud.ScoreClaimAsync(claimId, cancellationToken);
        }

        // 6. Istek garancije — čist datumski proračun (ne RAG).
        var warrantyExpired = purchase is not null && product is not null
            && WarrantyCalculator.IsExpired(purchase.PurchaseDate, product.WarrantyMonths, claim.SubmittedAt);

        var signals = new ClaimAnalysisResult(claimId, ocr, vision, policy, fraud, validation, warrantyExpired);

        // 7. Deterministička odluka.
        var decision = _gate.Evaluate(signals);

        // 8. Fiksiraj odluku (rizik + status) PRIJE objašnjenja.
        await _claims.ApplyDecisionAsync(claimId, fraud?.RiskScore, decision.Status);

        _logger.LogInformation(
            "Reklamacija {ClaimId}: odluka {Status} ({Code}).",
            claimId, decision.Status, decision.ReasonCode);

        // 9. LLM objašnjenje — best-effort; ne obara već fiksiranu odluku.
        ExplanationResult? explanation = null;
        try
        {
            explanation = await _explanation.ExplainAsync(decision, signals, claim, product, cancellationToken);
            await _claims.SetExplanationAsync(claimId, explanation.OperatorText);
            await _notifications.CreateAsync(new Notification
            {
                UserId = claim.UserId,
                ClaimId = claimId,
                Message = explanation.UserText,
                IsRead = false,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Odluka za reklamaciju {ClaimId} je upisana, ali objašnjenje/notifikacija nisu uspjeli.",
                claimId);
        }

        // 10. Snapshot nalaza za ekran detalja — uvijek (i bez objašnjenja).
        await _claims.SaveAnalysisAsync(claimId, BuildAnalysisJson(signals, decision, explanation));

        return decision;
    }

    private static string BuildAnalysisJson(
        ClaimAnalysisResult signals, DecisionResult decision, ExplanationResult? explanation)
    {
        var snapshot = new ClaimAnalysisSnapshot(
            ReceiptCheck: DescribeReceipt(signals.Validation),
            DamageCheck: DescribeDamage(signals.Vision),
            PolicyCheck: DescribePolicy(signals.Policy),
            RiskScore: signals.Fraud?.RiskScore,
            CustomerExplanation: explanation?.UserText,
            OperatorExplanation: explanation?.OperatorText,
            ReasonCode: decision.ReasonCode,
            Factors: decision.Factors,
            DecidedAt: DateTime.UtcNow);

        return JsonSerializer.Serialize(snapshot);
    }

    private static string? DescribeReceipt(ReceiptValidationResult? validation) => validation?.Status switch
    {
        ReceiptValidationStatus.Valid => "Validno — poklapa se s kupovinom",
        ReceiptValidationStatus.Mismatch => "Račun se ne poklapa (iznos ili datum)",
        ReceiptValidationStatus.NotFound => "Račun nije pronađen u sistemu",
        _ => null,
    };

    private static string? DescribeDamage(VisionResult? vision)
    {
        if (vision is null)
        {
            return null;
        }
        return vision.DamageConfirmed
            ? $"Potvrđeno: {vision.DamageType} ({vision.Severity})"
            : "Nije potvrđeno oštećenje";
    }

    private static string? DescribePolicy(RagResult? policy)
    {
        if (policy is null)
        {
            return null;
        }
        if (policy.ApplicableExclusion is not null)
        {
            return $"Mogući izuzetak: {policy.ApplicableExclusion}";
        }
        if (policy.Covered)
        {
            return policy.CitedArticle is not null ? $"Pokriveno ({policy.CitedArticle})" : "Pokriveno";
        }
        return "Pokrivenost nije jednoznačna";
    }
}
