namespace Reklio.Api.DTOs.Ai;

// Rezultat OCR ekstrakcije sa računa (implementacija: EPIC 6).
public record OcrResult(
    string? DocumentNumber,
    string? Branch,
    DateTime? PurchaseDate,
    decimal? Amount,
    string? ProductName,
    double Confidence,
    string? RawText);