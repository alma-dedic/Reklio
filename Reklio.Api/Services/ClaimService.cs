using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

public class ClaimService : IClaimService
{
    private readonly ReklioDbContext _db;

    public ClaimService(ReklioDbContext db)
    {
        _db = db;
    }

    // T2.3 state machine:
    // Submitted → Processing → {AutoApproved, AutoRejected, Escalated} → {OperatorApproved, OperatorRejected}
    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> AllowedTransitions =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.Submitted] = [ClaimStatus.Processing],
            [ClaimStatus.Processing] = [ClaimStatus.AutoApproved, ClaimStatus.AutoRejected, ClaimStatus.Escalated],
            [ClaimStatus.Escalated] = [ClaimStatus.OperatorApproved, ClaimStatus.OperatorRejected],
        };

    public async Task<Claim?> GetByIdAsync(int id)
    {
        return await _db.Claims.FindAsync(id);
    }

    public async Task<Claim> CreateAsync(Claim claim)
    {
        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();
        return claim;
    }

    public async Task UpdateStatusAsync(int claimId, ClaimStatus newStatus)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        if (!CanTransition(claim.Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Nevalidan prelaz statusa: {claim.Status} → {newStatus}.");
        }

        claim.Status = newStatus;
        await _db.SaveChangesAsync();
    }

    public async Task ApplyDecisionAsync(int claimId, double riskScore, ClaimStatus newStatus)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        if (!CanTransition(claim.Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Nevalidan prelaz statusa: {claim.Status} → {newStatus}.");
        }

        claim.RiskScore = riskScore;
        claim.Status = newStatus;
        await _db.SaveChangesAsync();
    }

    public async Task SetExplanationAsync(int claimId, string aiExplanation)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        claim.AiExplanation = aiExplanation;
        await _db.SaveChangesAsync();
    }

    private static bool CanTransition(ClaimStatus from, ClaimStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}