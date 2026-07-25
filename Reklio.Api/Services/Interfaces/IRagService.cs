using Reklio.Api.DTOs.Ai;

namespace Reklio.Api.Services.Interfaces;

// T4.1 — ugovor prema RAG provjeri pravilnika. Implementacija dolazi u EPIC 7.
public interface IRagService
{
    Task<RagResult> CheckPolicyAsync(RagQuery query, CancellationToken cancellationToken = default);
}