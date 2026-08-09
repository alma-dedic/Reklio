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
        var purchase = await _purchases.GetByIdAsync(claim.PurchaseId);
        var product = purchase is not null ? await _products.GetByIdAsync(purchase.ProductId) : null;

        // 1. OCR nad dokazom kupovine (samo fizički račun).
        OcrResult? ocr = null;
        var receipt = evidence.FirstOrDefault(e => e.Type == ReceiptEvidenceType);
        if (receipt is not null)
        {
            ocr = await _ocr.ExtractReceiptAsync(receipt.FilePath, cancellationToken);
        }

        // 2. Validacija dokaza kupovine protiv Purchase tabele.
        ReceiptValidationResult? validation = null;
        if (purchase is not null)
        {
            if (purchase.PurchaseType == PurchaseType.Online)
            {
                validation = await _validation.ValidateOnlineAsync(purchase.DocumentNumber);
            }
            else if (ocr is not null)
            {
                validation = await _validation.ValidateReceiptAsync(ocr);
            }
            // Fizička kupovina bez priloženog računa → validation ostaje null → gate eskalira.
        }

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

        // 4. Provjera pravilnika/garancije za kategoriju proizvoda (mek signal).
        RagResult? policy = null;
        if (purchase is not null)
        {
            var query = new RagQuery(
                Question: claim.IssueDescription,
                ProductCategory: product?.Category,
                IssueType: claim.IssueType);
            policy = await _rag.CheckPolicyAsync(query, cancellationToken);
        }

        // 5. Procjena rizika.
        var fraud = await _fraud.ScoreClaimAsync(claimId, cancellationToken);

        // 6. Istek garancije — čist datumski proračun (ne RAG).
        var warrantyExpired = purchase is not null && product is not null
            && WarrantyCalculator.IsExpired(purchase.PurchaseDate, product.WarrantyMonths, claim.SubmittedAt);

        var signals = new ClaimAnalysisResult(claimId, ocr, vision, policy, fraud, validation, warrantyExpired);

        // 7. Deterministička odluka.
        var decision = _gate.Evaluate(signals);

        // 8. Fiksiraj odluku (rizik + status) PRIJE objašnjenja.
        await _claims.ApplyDecisionAsync(claimId, fraud.RiskScore, decision.Status);

        _logger.LogInformation(
            "Reklamacija {ClaimId}: odluka {Status} ({Code}).",
            claimId, decision.Status, decision.ReasonCode);

        // 9. LLM objašnjenje + notifikacija — best-effort; ne obara već fiksiranu odluku.
        try
        {
            var explanation = await _explanation.ExplainAsync(decision, signals, claim, product, cancellationToken);
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

        return decision;
    }
}
