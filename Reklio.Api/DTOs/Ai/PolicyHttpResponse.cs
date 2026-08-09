using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python RAG endpointa (POST /analyze/policy).
public class PolicyHttpResponse
{
    [JsonPropertyName("covered")]
    public bool Covered { get; set; }

    [JsonPropertyName("applicable_exclusion")]
    public string? ApplicableExclusion { get; set; }

    [JsonPropertyName("cited_article")]
    public string? CitedArticle { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;
}
