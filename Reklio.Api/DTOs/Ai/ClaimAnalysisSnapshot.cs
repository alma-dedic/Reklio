namespace Reklio.Api.DTOs.Ai;

public record ClaimAnalysisSnapshot(
    string? ReceiptCheck,
    string? DamageCheck,
    string? PolicyCheck,
    double? RiskScore,
    string? CustomerExplanation,
    string? OperatorExplanation,
    string ReasonCode,
    IReadOnlyList<string> Factors,
    DateTime DecidedAt,
    // Bogati RAG nalaz za operatera (pokriveno + član + mogući izuzetak). Kupcu ide PolicyCheck (prosto).
    string? PolicyDetail = null,
    // Glavni faktori rizika (SHAP), mapirani u čitljive labele — samo za operatera.
    IReadOnlyList<string>? RiskFactors = null,
    // AI preporuka za eskalirane: lean smjera + draft razloga za kupca (prefill dijaloga).
    string? Recommendation = null,
    string? CustomerReasonDraft = null);