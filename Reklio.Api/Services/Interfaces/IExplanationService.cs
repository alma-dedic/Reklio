using Reklio.Api.DTOs.Ai;
using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

// T9.3 — LLM objašnjenje. Poziva se TEK nakon što je odluka fiksirana; LLM
// objašnjava odluku, ne donosi je. Vraća dva teksta (korisnik + operater).
public interface IExplanationService
{
    Task<ExplanationResult> ExplainAsync(
        DecisionResult decision,
        ClaimAnalysisResult signals,
        Claim claim,
        Product? product,
        CancellationToken cancellationToken = default);
}
