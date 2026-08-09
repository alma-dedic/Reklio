namespace Reklio.Api.DTOs.Validation;

public enum ReceiptValidationStatus
{
    Valid,
    Mismatch,
    NotFound
}

public record ReceiptValidationResult(
    ReceiptValidationStatus Status,
    int? PurchaseId,
    IReadOnlyList<string> Issues)
{
    public static ReceiptValidationResult Valid(int purchaseId) =>
        new(ReceiptValidationStatus.Valid, purchaseId, []);

    public static ReceiptValidationResult NotFound() =>
        new(ReceiptValidationStatus.NotFound, null, ["document_number"]);

    public static ReceiptValidationResult Mismatch(int purchaseId, IReadOnlyList<string> issues) =>
        new(ReceiptValidationStatus.Mismatch, purchaseId, issues);
}
