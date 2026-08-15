using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;
using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IReceiptValidationService
{
    // Online kupovina: lookup po broju dokumenta (tačno), bez OCR-a.
    Task<ReceiptValidationResult> ValidateOnlineAsync(string documentNumber);

    // Fizička kupovina: poredi sliku (OCR) sa izabranom kupovinom i njenim računom —
    // broj tačno, a iznos (suma svih stavki računa) i datum fuzzy.
    Task<ReceiptValidationResult> ValidateReceiptAsync(OcrResult ocr, Purchase selectedPurchase);
}
