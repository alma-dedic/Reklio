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
    DateTime DecidedAt);