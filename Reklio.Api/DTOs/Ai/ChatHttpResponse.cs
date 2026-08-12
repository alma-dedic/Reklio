using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python chat endpointa (POST /chat).
public class ChatHttpResponse
{
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("cited_article")]
    public string? CitedArticle { get; set; }
}
