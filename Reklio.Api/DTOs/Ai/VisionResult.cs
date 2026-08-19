namespace Reklio.Api.DTOs.Ai;

// Odgovara /analyze/damage (T8.2). DamageType, Severity i ProductType su iz fiksnih enum lista.
// ProductType = koji je proizvod prepoznat na slici (za provjeru poklapanja s izabranim).
public record VisionResult(
    bool DamageConfirmed,
    string DamageType,
    string Severity,
    double Confidence,
    string Description,
    string ProductType);
