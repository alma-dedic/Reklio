using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T6.6 — šalje sliku računa Python OCR endpointu i mapira odgovor na OcrResult.
public class OcrService : IOcrService
{
    private readonly HttpClient _http;

    public OcrService(HttpClient http)
    {
        _http = http;
    }

    public async Task<OcrResult> ExtractReceiptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _http.PostAsync("/analyze/receipt", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<OcrHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Prazan OCR odgovor.");

        DateTime? purchaseDate = DateTime.TryParse(
            dto.PurchaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

        return new OcrResult(
            dto.DocumentNumber, dto.Branch, purchaseDate, dto.Amount,
            dto.ProductName, dto.Confidence, dto.RawText);
    }
}
