using Reklio.Api.DTOs.Ai;

namespace Reklio.Api.Services.Interfaces;

// T9.1 — deterministički gate: čista funkcija nad agregiranim signalima, bez LLM-a.
public interface IDecisionGate
{
    DecisionResult Evaluate(ClaimAnalysisResult signals);
}
