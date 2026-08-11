using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IClaimEvidenceService
{
    Task<ClaimEvidence> AddToClaimAsync(ClaimEvidence evidence);

    Task<ClaimEvidence?> GetByIdAsync(int id);

    Task<IReadOnlyList<ClaimEvidence>> GetByClaimAsync(int claimId);
}