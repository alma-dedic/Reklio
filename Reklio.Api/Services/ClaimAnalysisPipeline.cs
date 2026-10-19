using System.Text.Json;
using Reklio.Api.Data;
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
    private const string PhotoEvidenceType = "Photo";

    private readonly IVisionService _vision;
    private readonly IRagService _rag;
    private readonly IFraudService _fraud;
    private readonly IDecisionGate _gate;
    private readonly IExplanationService _explanation;
    private readonly IClaimService _claims;
    private readonly IClaimEvidenceService _evidence;
    private readonly IPurchaseService _purchases;
    private readonly IProductService _products;
    private readonly INotificationService _notifications;
    private readonly ILogger<ClaimAnalysisPipeline> _logger;

    public ClaimAnalysisPipeline(
        IVisionService vision,
        IRagService rag,
        IFraudService fraud,
        IDecisionGate gate,
        IExplanationService explanation,
        IClaimService claims,
        IClaimEvidenceService evidence,
        IPurchaseService purchases,
        IProductService products,
        INotificationService notifications,
        ILogger<ClaimAnalysisPipeline> logger)
    {
        _vision = vision;
        _rag = rag;
        _fraud = fraud;
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

        // Kupovina je obavezna i razriješena pri unosu (Submit to nameće) → ovdje uvijek postoji.
        var purchase = claim.PurchaseId is int linkedId
            ? await _purchases.GetByIdAsync(linkedId)
            : null;
        if (purchase is null)
        {
            throw new InvalidOperationException($"Reklamacija {claimId} nema razriješenu kupovinu.");
        }

        // 1. Račun je OCR-ovan i validiran pri unosu (resolve) → ovdje je uvijek validno.
        var validation = ReceiptValidationResult.Valid(purchase.Id);

        var product = await _products.GetByIdAsync(purchase.ProductId);

        // 2. Vizuelna analiza fotografija oštećenja.
        VisionResult? vision = null;
        var photos = evidence
            .Where(e => e.Type == PhotoEvidenceType)
            .Select(e => e.FilePath)
            .ToList();
        if (photos.Count > 0)
        {
            vision = await _vision.AnalyzeDamageAsync(photos, cancellationToken);
        }

        // 3. Provjera pravilnika (mek signal). Upit se gradi iz vizuelnog nalaza kad on
        //    postoji — opis kupca je samoprijavljen (docs/analiza-povjerenja-ulaza.md, N4).
        var policyQuestion = vision is { DamageConfirmed: true }
            ? $"Fizičko oštećenje: {DamageTypeLabel(vision.DamageType)}, {SeverityLabel(vision.Severity)}"
            : claim.IssueDescription;

        var query = new RagQuery(
            Question: policyQuestion,
            ProductCategory: product?.Category,
            IssueType: claim.IssueType);
        var policy = await _rag.CheckPolicyAsync(query, cancellationToken);

        // 4. Procjena rizika (fn_features nad kupovinom/reklamacijom).
        var fraud = await _fraud.ScoreClaimAsync(claimId, cancellationToken);

        // 5. Istek garancije — čist datumski proračun (ne RAG).
        var warrantyExpired = product is not null
            && WarrantyCalculator.IsExpired(purchase.PurchaseDate, product.WarrantyMonths, claim.SubmittedAt);

        // 5b. Slika ne odgovara izabranom proizvodu — samo kad je vizija sigurna u tip (mek signal).
        var productMismatch = product is not null && vision is not null
            && !string.IsNullOrEmpty(vision.ProductType)
            && !string.Equals(vision.ProductType, "Nepoznato", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(vision.ProductType, CatalogSeed.TypeFor(product), StringComparison.OrdinalIgnoreCase);

        var signals = new ClaimAnalysisResult(
            claimId, null, vision, policy, fraud, validation, warrantyExpired, productMismatch,
            claim.IssueType);

        // 7. Deterministička odluka.
        var decision = _gate.Evaluate(signals);

        // 8. Fiksiraj odluku (rizik + status) PRIJE objašnjenja.
        await _claims.ApplyDecisionAsync(claimId, fraud?.RiskScore, decision.Status);

        _logger.LogInformation(
            "Reklamacija {ClaimId}: odluka {Status} ({Code}).",
            claimId, decision.Status, decision.ReasonCode);

        // 9. Determinirane poruke ishoda kupcu (obavještenje + obrazloženje + naredni korak).
        var reference = $"REK-{claim.SubmittedAt.Year}-{claim.Id:D5}";
        var reason = decision.Status == ClaimStatus.AutoRejected
            ? ClaimOutcomeMessages.ReasonForCode(decision.ReasonCode)
            : null;
        var customerText = ClaimOutcomeMessages.Detail(decision.Status, reason, reference, claim.IssueType);

        // Kratka notifikacija — deterministička, ne zavisi od LLM-a.
        await _notifications.CreateAsync(new Notification
        {
            UserId = claim.UserId,
            ClaimId = claimId,
            Message = ClaimOutcomeMessages.Notification(decision.Status, reason, reference),
            IsRead = false,
        });

        // 10. AI preporuka — SAMO za eskalirane (njih pregleda operater). Best-effort.
        //     Auto-odluke ne trebaju LLM: kupcu idu determinističke poruke, operatera nema.
        string? recommendation = null;
        string? operatorText = null;
        string? customerReasonDraft = null;
        if (decision.Status == ClaimStatus.Escalated)
        {
            try
            {
                var explanation = await _explanation.ExplainAsync(decision, signals, claim, product, cancellationToken);
                recommendation = explanation.Recommendation;
                operatorText = explanation.OperatorText;
                customerReasonDraft = explanation.CustomerReason;
                await _claims.SetExplanationAsync(claimId, operatorText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Reklamacija {ClaimId} je eskalirana, ali AI preporuka za operatera nije uspjela.",
                    claimId);
            }
        }

        // 11. Snapshot nalaza za ekran detalja.
        await _claims.SaveAnalysisAsync(
            claimId,
            BuildAnalysisJson(
                signals, decision, customerText, operatorText, recommendation, customerReasonDraft,
                purchase.PurchaseType));

        return decision;
    }

    private static string BuildAnalysisJson(
        ClaimAnalysisResult signals, DecisionResult decision, string customerText, string? operatorText,
        string? recommendation, string? customerReasonDraft, PurchaseType channel)
    {
        var snapshot = new ClaimAnalysisSnapshot(
            ReceiptCheck: DescribeReceipt(signals.Validation, channel),
            DamageCheck: DescribeDamage(signals.Vision),
            PolicyCheck: DescribePolicy(signals.Policy),
            PolicyDetail: DescribePolicyDetail(signals.Policy),
            RiskScore: signals.Fraud?.RiskScore,
            RiskFactors: MapRiskFactors(signals.Fraud?.TopFactors),
            CustomerExplanation: customerText,
            OperatorExplanation: operatorText,
            ReasonCode: decision.ReasonCode,
            Factors: decision.Factors,
            DecidedAt: DateTime.UtcNow,
            Recommendation: recommendation,
            CustomerReasonDraft: customerReasonDraft);

        return JsonSerializer.Serialize(snapshot);
    }

    // Online nema sliku računa, pa se iznos i datum ne provjeravaju — poruka to razlikuje.
    private static string? DescribeReceipt(ReceiptValidationResult? validation, PurchaseType channel) =>
        validation?.Status switch
        {
            ReceiptValidationStatus.Valid when channel == PurchaseType.Online =>
                "Validno — narudžba pronađena u sistemu",
            ReceiptValidationStatus.Valid =>
                "Validno — broj, iznos i datum sa računa se poklapaju",
            ReceiptValidationStatus.NotFound => "Račun nije pronađen u sistemu",
            _ => null,
        };

    private static string? DescribeDamage(VisionResult? vision)
    {
        if (vision is null)
        {
            return null;
        }
        var seen = string.IsNullOrEmpty(vision.ProductType) || vision.ProductType == "Nepoznato"
            ? string.Empty
            : $" · prepoznato: {vision.ProductType}";
        return vision.DamageConfirmed
            ? $"Potvrđeno — {DamageTypeLabel(vision.DamageType)}, {SeverityLabel(vision.Severity)}{seen}"
            : $"Nije potvrđeno oštećenje{seen}";
    }

    // Enum kodovi vizije → bosanski za prikaz (kod ostaje engleski za grananje u gate-u).
    private static string DamageTypeLabel(string type) => type switch
    {
        "ScreenCrack" => "napuknut ekran",
        "Dent" => "udubljenje",
        "Scratch" => "ogrebotina",
        "ConnectorDamage" => "oštećen konektor",
        "PhysicalBreak" => "fizički lom",
        "WaterDamage" => "oštećenje vodom",
        "Swelling" => "bubrenje",
        "Other" => "drugo oštećenje",
        _ => type.ToLowerInvariant(),
    };

    private static string SeverityLabel(string severity) => severity switch
    {
        "Mild" => "blago",
        "Moderate" => "umjereno",
        "Severe" => "teško",
        _ => severity.ToLowerInvariant(),
    };

    // Kupcu: prosto, bez izuzetka (da ne uznemiri prije nego operater odluči).
    private static string? DescribePolicy(RagResult? policy)
    {
        if (policy is null)
        {
            return null;
        }
        var cited = CleanCitation(policy.CitedArticle);
        if (policy.Covered)
        {
            return cited is not null ? $"Pokriveno ({cited})" : "Pokriveno";
        }
        return "Pokrivenost nije jednoznačna";
    }

    // RAG citira u obliku "02_garantni_rokovi_kategorije.md — Član 2"; naziv datoteke
    // ne treba ni kupcu ni operateru, pa se pretvara u čitljiv naslov.
    private static string? CleanCitation(string? cited)
    {
        if (string.IsNullOrWhiteSpace(cited))
        {
            return null;
        }

        var parts = cited.Split('—', 2, StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return cited.Trim();
        }

        var source = Path.GetFileNameWithoutExtension(parts[0]).Replace('_', ' ').Trim();
        source = string.Concat(source.SkipWhile(c => char.IsDigit(c) || c == ' '));
        if (source.Length > 0)
        {
            source = char.ToUpperInvariant(source[0]) + source[1..];
        }

        return source.Length > 0 ? $"{source} — {parts[1]}" : parts[1];
    }

    // Operateru: pokriveno + član + mogući izuzetak + obrazloženje RAG-a (zašto).
    private static string? DescribePolicyDetail(RagResult? policy)
    {
        if (policy is null)
        {
            return null;
        }

        var cited = CleanCitation(policy.CitedArticle);
        var parts = new List<string>
        {
            policy.Covered
                ? (cited is not null ? $"Pokriveno ({cited})" : "Pokriveno")
                : "Pokrivenost nije jednoznačna",
        };

        if (policy.ApplicableExclusion is not null)
        {
            parts.Add($"mogući izuzetak: {policy.ApplicableExclusion}");
        }

        var text = string.Join(" · ", parts);
        return string.IsNullOrWhiteSpace(policy.Answer) ? text : $"{text} — {policy.Answer}";
    }

    // SHAP top-faktori rizika → čitljive bosanske labele (operateru, u zagradama uz rizik).
    private static IReadOnlyList<string>? MapRiskFactors(IReadOnlyList<string>? factors)
    {
        if (factors is null || factors.Count == 0)
        {
            return null;
        }
        return factors.Select(RiskFactorLabel).ToList();
    }

    private static string RiskFactorLabel(string feature) => feature switch
    {
        "prior_claims_on_purchase" => "ranije reklamacije na istoj kupovini",
        "purchase_claimed_by_other_account" => "kupovinu reklamirao drugi nalog",
        "distinct_accounts_on_purchase" => "više naloga na istoj kupovini",
        "days_purchase_to_claim" => "vrijeme od kupovine do reklamacije",
        "warranty_period_used_pct" => "iskorišten dio garancije",
        "claimed_within_first_n_days" => "reklamacija odmah nakon kupovine",
        "total_claims" => "ukupan broj reklamacija",
        "claims_last_30d" => "reklamacije u zadnjih 30 dana",
        "claims_last_90d" => "reklamacije u zadnjih 90 dana",
        "mean_days_between_claims" => "razmak između reklamacija",
        "account_age_days" => "starost naloga",
        "prior_rejection_rate" => "raniji udio odbijenih",
        "claim_amount" => "iznos reklamacije",
        "amount_vs_user_mean" => "iznos naspram prosjeka korisnika",
        "distinct_categories" => "broj različitih kategorija",
        "distinct_stores" => "broj različitih prodavnica",
        _ => feature,
    };
}
