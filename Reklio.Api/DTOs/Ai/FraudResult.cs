namespace Reklio.Api.DTOs.Ai;

// Procjena rizika reklamacije (implementacija: EPIC 5, XGBoost nad fn_features).
// RiskScore je vjerovatnoća 0.0 - 1.0.
public record FraudResult(
    double RiskScore,
    IReadOnlyList<string> TopFactors);