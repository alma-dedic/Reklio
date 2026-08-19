using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T6.4 / T6.5 — validacija računa protiv Purchase tabele (na unosu/resolve).
public class ReceiptValidationService : IReceiptValidationService
{
    // Fuzzy tolerancije: apsorbuju OCR šum, ali velika razlika = neispravan račun.
    private const decimal AmountToleranceKm = 1.00m;
    private const int DateToleranceDays = 1;

    private readonly IPurchaseService _purchases;

    public ReceiptValidationService(IPurchaseService purchases)
    {
        _purchases = purchases;
    }

    public async Task<ReceiptValidationResult> ValidateDocumentAsync(OcrResult ocr)
    {
        if (string.IsNullOrWhiteSpace(ocr.DocumentNumber))
        {
            return ReceiptValidationResult.NotFound();
        }

        var lines = await _purchases.FindAllByDocumentNumberAsync(ocr.DocumentNumber.Trim());
        if (lines.Count == 0)
        {
            return ReceiptValidationResult.NotFound();
        }

        // Iznos sa slike je TOTAL računa → poredi sa sumom svih stavki dokumenta.
        var total = lines.Sum(l => l.Amount);
        var purchaseDate = lines[0].PurchaseDate;

        var issues = new List<string>();
        if (ocr.Amount is null || Math.Abs(ocr.Amount.Value - total) > AmountToleranceKm)
        {
            issues.Add("amount");
        }
        if (ocr.PurchaseDate is null ||
            Math.Abs((ocr.PurchaseDate.Value.Date - purchaseDate.Date).TotalDays) > DateToleranceDays)
        {
            issues.Add("date");
        }

        return issues.Count == 0
            ? ReceiptValidationResult.Valid(lines[0].Id)
            : ReceiptValidationResult.Mismatch(lines[0].Id, issues);
    }
}
