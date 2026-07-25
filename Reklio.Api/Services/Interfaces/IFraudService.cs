using Reklio.Api.DTOs.Ai;

namespace Reklio.Api.Services.Interfaces;

// T4.1 — ugovor prema fraud modelu. Implementacija dolazi u EPIC 5.
// Ulaz je ClaimId jer se feature-i računaju SQL funkcijom fn_features.
public interface IFraudService
{
    Task<FraudResult> ScoreClaimAsync(int claimId, CancellationToken cancellationToken = default);
}