using Reklio.Api.DTOs.Validation;

namespace Reklio.Api.DTOs.Ai;

// Agregirani rezultat cijelog pipeline-a — ulaz za decision gate (EPIC 9).
// Pojedinačni dijelovi su null ako korak nije bio primjenjiv (npr. nema fotografija).
// WarrantyExpired je čist datumski proračun (ne RAG) — vidi WarrantyCalculator.
public record ClaimAnalysisResult(
    int ClaimId,
    OcrResult? Ocr,
    VisionResult? Vision,
    RagResult? Policy,
    FraudResult? Fraud,
    ReceiptValidationResult? Validation,
    bool WarrantyExpired,
    bool ProductMismatch);
