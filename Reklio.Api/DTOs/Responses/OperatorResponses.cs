namespace Reklio.Api.DTOs.Responses;

// Red eskaliranih (lista) — dovoljno za pregled i sortiranje po riziku.
public record OperatorClaimSummary(
    int Id,
    string Reference,
    string ProductName,
    string CustomerName,
    double? RiskScore,
    DateTime SubmittedAt);

public record OperatorEvidenceItem(
    int Id,
    string Type);

// Pun detalj za operatera — sve što treba za odluku na jednom mjestu.
public record OperatorClaimDetail(
    int Id,
    string Reference,
    string ProductName,
    string CustomerName,
    string CustomerEmail,
    string Status,
    DateTime SubmittedAt,
    string IssueType,
    string IssueDescription,
    ClaimAnalysisResponse? Analysis,
    string? OperatorExplanation,
    string? Recommendation,
    string? CustomerReason,
    IReadOnlyList<string> Factors,
    IReadOnlyList<OperatorEvidenceItem> Evidence);
