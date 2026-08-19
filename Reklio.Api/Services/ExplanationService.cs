using System.Net.Http.Json;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T9.3 — šalje fiksiranu odluku + sve signale Python explanation endpointu.
// Determinizam odluke je već zaključan; ovdje LLM samo formuliše dva teksta.
public class ExplanationService : IExplanationService
{
    private readonly HttpClient _http;

    public ExplanationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ExplanationResult> ExplainAsync(
        DecisionResult decision,
        ClaimAnalysisResult signals,
        Claim claim,
        Product? product,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            decision = decision.Status.ToString(),
            reason_code = decision.ReasonCode,
            factors = decision.Factors,
            validation_status = signals.Validation?.Status.ToString(),
            warranty_expired = signals.WarrantyExpired,
            risk_score = signals.Fraud?.RiskScore ?? 0.0,
            risk_threshold = DecisionGate.RiskThreshold,
            damage_confirmed = signals.Vision?.DamageConfirmed,
            damage_type = signals.Vision?.DamageType,
            severity = signals.Vision?.Severity,
            policy_covered = signals.Policy?.Covered,
            applicable_exclusion = signals.Policy?.ApplicableExclusion,
            cited_article = signals.Policy?.CitedArticle,
            policy_answer = signals.Policy?.Answer,
            issue_type = claim.IssueType,
            issue_description = claim.IssueDescription,
            product_name = product?.Name,
            product_category = product?.Category,
        };

        var response = await _http.PostAsJsonAsync("/explain/decision", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ExplanationHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Prazan odgovor explanation servisa.");

        return new ExplanationResult(dto.Recommendation, dto.OperatorText, dto.CustomerReason);
    }
}
