using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;

namespace Reklio.Api.Services.Interfaces;

public interface IReceiptValidationService
{
    // Validacija računa pri unosu (in-store, resolve): OCR sa slike vs dokument u bazi —
    // broj postoji + iznos (suma stavki) i datum u toleranciji. Jedina validacija/OCR tačka.
    Task<ReceiptValidationResult> ValidateDocumentAsync(OcrResult ocr);
}
