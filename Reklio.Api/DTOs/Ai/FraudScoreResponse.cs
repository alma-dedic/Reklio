using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python fraud servisa (POST /fraud/score).
public class FraudScoreResponse
{
    [JsonPropertyName("risk_score")]
    public double RiskScore { get; set; }

    [JsonPropertyName("top_factors")]
    public IReadOnlyList<string> TopFactors { get; set; } = [];
}
