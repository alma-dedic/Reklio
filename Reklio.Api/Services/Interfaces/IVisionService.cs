using Reklio.Api.DTOs.Ai;

namespace Reklio.Api.Services.Interfaces;

// T4.1 — ugovor prema vizuelnoj analizi. Implementacija dolazi u EPIC 8.
public interface IVisionService
{
    Task<VisionResult> AnalyzeDamageAsync(
        IReadOnlyList<string> imagePaths,
        CancellationToken cancellationToken = default);
}