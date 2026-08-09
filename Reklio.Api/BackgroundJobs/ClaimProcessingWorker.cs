using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.BackgroundJobs;

// T3.1 + T3.3 — čita ClaimId iz reda i obrađuje (placeholder). Pri startu vraća
// zaglavljene reklamacije (Submitted/Processing) nazad u red.
public class ClaimProcessingWorker : BackgroundService
{
    private readonly IClaimQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClaimProcessingWorker> _logger;

    public ClaimProcessingWorker(
        IClaimQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ClaimProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverStuckClaimsAsync(stoppingToken);

        try
        {
            await foreach (var claimId in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessClaimAsync(claimId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Greška pri obradi reklamacije {ClaimId}.", claimId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Uredno gašenje aplikacije.
        }
    }

    // Crash-recovery (T3.3): sve što je bilo "u letu" vrati u svježi red.
    // Hvatamo i Submitted (bila u redu, worker je nije stigao uzeti) i Processing
    // (worker je bio usred obrade) — oba bi inače bila izgubljena jer je red u RAM-u.
    private async Task RecoverStuckClaimsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReklioDbContext>();

        var stuckIds = await db.Claims
            .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.Processing)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in stuckIds)
        {
            await _queue.EnqueueAsync(id, cancellationToken);
        }

        if (stuckIds.Count > 0)
        {
            _logger.LogInformation("Crash-recovery: vraćeno {Count} reklamacija u red.", stuckIds.Count);
        }
    }

    private async Task ProcessClaimAsync(int claimId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var claims = scope.ServiceProvider.GetRequiredService<IClaimService>();

        var claim = await claims.GetByIdAsync(claimId);
        if (claim is null)
        {
            _logger.LogWarning("Reklamacija {ClaimId} više ne postoji, preskačem.", claimId);
            return;
        }

        // Worker obrađuje samo aktivne reklamacije; sve ostalo (Escalated/konačno) preskoči.
        if (claim.Status is not (ClaimStatus.Submitted or ClaimStatus.Processing))
        {
            return;
        }

        if (claim.Status == ClaimStatus.Submitted)
        {
            await claims.UpdateStatusAsync(claimId, ClaimStatus.Processing);
        }

        // T9.4 — pun AI pipeline: signali → deterministički gate → fiksna odluka → objašnjenje.
        var pipeline = scope.ServiceProvider.GetRequiredService<ClaimAnalysisPipeline>();
        var decision = await pipeline.RunAsync(claimId, cancellationToken);

        _logger.LogInformation(
            "Reklamacija {ClaimId} obrađena: {Status} ({Code}).",
            claimId, decision.Status, decision.ReasonCode);
    }
}