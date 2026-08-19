namespace Reklio.Api.DTOs.Responses;

// Odgovori ka Angular klijentu. System.Text.Json serijalizuje u camelCase,
// pa se poklapa sa frontend modelima (id, productName, status...).
public record ClaimSummaryResponse(
    int Id,
    string Reference,
    string ProductName,
    string Status,
    DateTime SubmittedAt);

public record ClaimAnalysisResponse(
    string? ReceiptCheck,
    string? DamageCheck,
    string? PolicyCheck,
    double? RiskScore,
    // Glavni faktori rizika (SHAP) — samo operateru; kupcu ide null.
    IReadOnlyList<string>? RiskFactors = null);

public record ClaimDetailResponse(
    int Id,
    string Reference,
    string ProductName,
    string Status,
    DateTime SubmittedAt,
    string IssueType,
    string IssueDescription,
    string? Explanation,
    ClaimAnalysisResponse? Analysis);