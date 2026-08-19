using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python explanation endpointa (POST /explain/decision).
public class ExplanationHttpResponse
{
    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("operator_text")]
    public string OperatorText { get; set; } = string.Empty;

    [JsonPropertyName("customer_reason")]
    public string CustomerReason { get; set; } = string.Empty;
}
