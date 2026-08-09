using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;

namespace Reklio.Api.Services.Interfaces;

public interface IReceiptValidationService
{
    // Online kupovina: lookup po broju dokumenta (tačno), bez OCR-a.
    Task<ReceiptValidationResult> ValidateOnlineAsync(string documentNumber);

    // Fizička kupovina: poredi OCR sa Purchase — doc tačno, iznos/datum fuzzy.
    Task<ReceiptValidationResult> ValidateReceiptAsync(OcrResult ocr);
}
