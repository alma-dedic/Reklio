using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python explanation endpointa (POST /explain/decision).
public class ExplanationHttpResponse
{
    [JsonPropertyName("user_text")]
    public string UserText { get; set; } = string.Empty;

    [JsonPropertyName("operator_text")]
    public string OperatorText { get; set; } = string.Empty;
}
