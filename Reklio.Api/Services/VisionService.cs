using System.Net.Http.Headers;
using System.Net.Http.Json;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T8.3 — analizira fotografije oštećenja preko Python vision endpointa.
// Za više fotografija vraća najozbiljnije potvrđeno oštećenje.
public class VisionService : IVisionService
{
    private readonly HttpClient _http;

    public VisionService(HttpClient http)
    {
        _http = http;
    }

    public async Task<VisionResult> AnalyzeDamageAsync(
        IReadOnlyList<string> imagePaths, CancellationToken cancellationToken = default)
    {
        VisionResult? worst = null;
        VisionResult? any = null;
        var worstRank = -1;

        foreach (var path in imagePaths)
        {
            var result = await AnalyzeOneAsync(path, cancellationToken);
            any = result;   // zadnja analizirana — nosi prepoznati tip i kad nema oštećenja
            var rank = SeverityRank(result.Severity);
            if (result.DamageConfirmed && rank > worstRank)
            {
                worstRank = rank;
                worst = result;
            }
        }

        // Najgore potvrđeno oštećenje; ako ga nema, zadrži tip sa slike (za provjeru poklapanja).
        return worst ?? any ?? new VisionResult(false, "None", "None", 0, "Nema vidljivog oštećenja.", "Nepoznato");
    }

    private async Task<VisionResult> AnalyzeOneAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _http.PostAsync("/analyze/damage", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<DamageHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Prazan vision odgovor.");

        return new VisionResult(
            dto.DamageConfirmed, dto.DamageType, dto.Severity, dto.Confidence, dto.Description, dto.ProductType);
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "Severe" => 3,
        "Moderate" => 2,
        "Mild" => 1,
        _ => 0,
    };
}
