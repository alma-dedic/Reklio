namespace Reklio.Api.DTOs.Ai;

// Rezultat vizuelne analize fotografija oštećenja (implementacija: EPIC 8).
public record VisionResult(
    bool DamageDetected,
    string? DamageType,
    string? Severity,
    double Confidence,
    string? Description);