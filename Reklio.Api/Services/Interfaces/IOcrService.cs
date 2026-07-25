using Reklio.Api.DTOs.Ai;

namespace Reklio.Api.Services.Interfaces;

// T4.1 — ugovor prema OCR komponenti. Implementacija dolazi u EPIC 6.
public interface IOcrService
{
    Task<OcrResult> ExtractReceiptAsync(string filePath, CancellationToken cancellationToken = default);
}