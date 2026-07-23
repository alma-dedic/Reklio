using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

public class ClaimEvidenceService : IClaimEvidenceService
{
    private readonly ReklioDbContext _db;

    public ClaimEvidenceService(ReklioDbContext db)
    {
        _db = db;
    }

    public async Task<ClaimEvidence> AddToClaimAsync(ClaimEvidence evidence)
    {
        _db.ClaimEvidence.Add(evidence);
        await _db.SaveChangesAsync();
        return evidence;
    }

    public async Task<IReadOnlyList<ClaimEvidence>> GetByClaimAsync(int claimId)
    {
        return await _db.ClaimEvidence
            .AsNoTracking()
            .Where(e => e.ClaimId == claimId)
            .ToListAsync();
    }
}