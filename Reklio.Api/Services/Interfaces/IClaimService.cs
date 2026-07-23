using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IClaimService
{
    Task<Claim?> GetByIdAsync(int id);

    Task<Claim> CreateAsync(Claim claim);

    // Mijenja status uz provjeru state machine-a (T2.3). Baca ako je prelaz nevalidan.
    Task UpdateStatusAsync(int claimId, ClaimStatus newStatus);
}