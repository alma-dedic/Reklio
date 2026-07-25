namespace Reklio.Api.DTOs.Ai;

// Agregirani rezultat cijelog pipeline-a — ulaz za decision gate (EPIC 9).
// Pojedinačni dijelovi su null ako korak nije bio primjenjiv (npr. nema fotografija).
public record ClaimAnalysisResult(
    int ClaimId,
    OcrResult? Ocr,
    VisionResult? Vision,
    RagResult? Policy,
    FraudResult? Fraud);