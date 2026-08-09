namespace Reklio.Api.DTOs.Ai;

// Odgovara /analyze/damage (T8.2). DamageType i Severity su iz fiksnih enum lista.
public record VisionResult(
    bool DamageConfirmed,
    string DamageType,
    string Severity,
    double Confidence,
    string Description);
