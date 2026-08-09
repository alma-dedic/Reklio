using System.Net.Http.Json;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T7.6 — pita Python RAG endpoint da li reklamacija odgovara pravilniku.
public class RagService : IRagService
{
    private readonly HttpClient _http;

    public RagService(HttpClient http)
    {
        _http = http;
    }

    public async Task<RagResult> CheckPolicyAsync(RagQuery query, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            question = query.Question,
            product_category = query.ProductCategory,
            issue_type = query.IssueType,
        };

        var response = await _http.PostAsJsonAsync("/analyze/policy", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<PolicyHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Prazan RAG odgovor.");

        return new RagResult(dto.Covered, dto.ApplicableExclusion, dto.CitedArticle, dto.Answer);
    }
}
