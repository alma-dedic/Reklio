using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python vision endpointa (POST /analyze/damage).
public class DamageHttpResponse
{
    [JsonPropertyName("damage_confirmed")]
    public bool DamageConfirmed { get; set; }

    [JsonPropertyName("damage_type")]
    public string DamageType { get; set; } = "None";

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "None";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
