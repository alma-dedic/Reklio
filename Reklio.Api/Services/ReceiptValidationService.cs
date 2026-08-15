using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T6.4 / T6.5 — validacija dokaza kupovine protiv Purchase tabele.
public class ReceiptValidationService : IReceiptValidationService
{
    // Fuzzy tolerancije: apsorbuju OCR šum, ali velika razlika = fraud signal.
    private const decimal AmountToleranceKm = 1.00m;
    private const int DateToleranceDays = 1;

    private readonly IPurchaseService _purchases;

    public ReceiptValidationService(IPurchaseService purchases)
    {
        _purchases = purchases;
    }

    public async Task<ReceiptValidationResult> ValidateOnlineAsync(string documentNumber)
    {
        var purchase = await _purchases.FindByDocumentNumberAsync(documentNumber.Trim());
        return purchase is null
            ? ReceiptValidationResult.NotFound()
            : ReceiptValidationResult.Valid(purchase.Id);
    }

    public async Task<ReceiptValidationResult> ValidateReceiptAsync(OcrResult ocr, Purchase selectedPurchase)
    {
        // Broj sa slike mora odgovarati izabranoj kupovini (hvata zamjenu slike računa).
        if (string.IsNullOrWhiteSpace(ocr.DocumentNumber) ||
            !string.Equals(ocr.DocumentNumber.Trim(), selectedPurchase.DocumentNumber,
                StringComparison.OrdinalIgnoreCase))
        {
            return ReceiptValidationResult.Mismatch(selectedPurchase.Id, ["document_number"]);
        }

        // Iznos sa slike je TOTAL računa → poredi sa sumom svih stavki tog dokumenta.
        var lines = await _purchases.FindAllByDocumentNumberAsync(selectedPurchase.DocumentNumber);
        var total = lines.Sum(l => l.Amount);

        var issues = new List<string>();
        if (ocr.Amount is null || Math.Abs(ocr.Amount.Value - total) > AmountToleranceKm)
        {
            issues.Add("amount");
        }
        if (ocr.PurchaseDate is null ||
            Math.Abs((ocr.PurchaseDate.Value.Date - selectedPurchase.PurchaseDate.Date).TotalDays) > DateToleranceDays)
        {
            issues.Add("date");
        }

        return issues.Count == 0
            ? ReceiptValidationResult.Valid(selectedPurchase.Id)
            : ReceiptValidationResult.Mismatch(selectedPurchase.Id, issues);
    }
}
