using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.DTOs.Ai;
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

    public async Task<IReadOnlyList<Claim>> GetByUserAsync(string userId)
    {
        return await _db.Claims
            .AsNoTracking()
            .Include(c => c.Purchase!).ThenInclude(p => p.Product)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Claim?> GetDetailAsync(int id)
    {
        return await _db.Claims
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Purchase!).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IReadOnlyList<Claim>> GetEscalatedAsync()
    {
        return await _db.Claims
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Purchase!).ThenInclude(p => p.Product)
            .Where(c => c.Status == ClaimStatus.Escalated)
            .OrderByDescending(c => c.SubmittedAt)   // najnovije prvo
            .ToListAsync();
    }

    public async Task ResolveByOperatorAsync(int claimId, string operatorId, ClaimStatus newStatus)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        if (!CanTransition(claim.Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Nevalidan prelaz statusa: {claim.Status} → {newStatus}.");
        }

        claim.OperatorId = operatorId;
        claim.Status = newStatus;
        await _db.SaveChangesAsync();
    }

    public async Task LinkPurchaseAsync(int claimId, int purchaseId)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        claim.PurchaseId = purchaseId;
        await _db.SaveChangesAsync();
    }

    public async Task SaveAnalysisAsync(int claimId, string analysisJson)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        claim.AnalysisJson = analysisJson;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateCustomerExplanationAsync(int claimId, string text)
    {
        var claim = await _db.Claims.FindAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        if (string.IsNullOrWhiteSpace(claim.AnalysisJson))
        {
            return;
        }

        var snapshot = JsonSerializer.Deserialize<ClaimAnalysisSnapshot>(claim.AnalysisJson);
        if (snapshot is null)
        {
            return;
        }

        claim.AnalysisJson = JsonSerializer.Serialize(snapshot with { CustomerExplanation = text });
        await _db.SaveChangesAsync();
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

    public async Task ApplyDecisionAsync(int claimId, double? riskScore, ClaimStatus newStatus)
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