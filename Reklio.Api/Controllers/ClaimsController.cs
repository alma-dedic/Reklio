using System.Text.Json;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.BackgroundJobs;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Requests;
using Reklio.Api.DTOs.Responses;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private const int MaxPhotos = 3;

    private readonly IClaimService _claims;
    private readonly IPurchaseService _purchases;
    private readonly IClaimEvidenceService _evidence;
    private readonly IFileStorageService _storage;
    private readonly IClaimQueue _queue;

    public ClaimsController(
        IClaimService claims,
        IPurchaseService purchases,
        IClaimEvidenceService evidence,
        IFileStorageService storage,
        IClaimQueue queue)
    {
        _claims = claims;
        _purchases = purchases;
        _evidence = evidence;
        _storage = storage;
        _queue = queue;
    }

    // T3.2 — vrati 202 odmah, obradu prepusti redu čekanja.
    // Online: broj računa se razrješava sad. Fizička: slika ide u pipeline (OCR razriješi kupovinu).
    [HttpPost]
    public async Task<IActionResult> Submit([FromForm] CreateClaimRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isOnline = string.Equals(request.PurchaseType, "Online", StringComparison.OrdinalIgnoreCase);
        int? purchaseId = null;

        if (isOnline)
        {
            if (string.IsNullOrWhiteSpace(request.DocumentNumber))
            {
                return BadRequest(new { message = "Broj računa je obavezan za online kupovinu." });
            }

            var purchase = await _purchases.FindByDocumentNumberAsync(request.DocumentNumber.Trim());
            if (purchase is null)
            {
                return BadRequest(new { message = "Kupovina s tim brojem računa nije pronađena." });
            }

            purchaseId = purchase.Id;
        }
        else if (request.Receipt is null)
        {
            return BadRequest(new { message = "Slika računa je obavezna za fizičku kupovinu." });
        }

        var claim = new Claim
        {
            UserId = userId,
            PurchaseId = purchaseId,
            IssueType = request.IssueType,
            IssueDescription = request.IssueDescription,
            Status = ClaimStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
        };
        await _claims.CreateAsync(claim);

        try
        {
            if (!isOnline && request.Receipt is not null)
            {
                var path = await _storage.SaveClaimFileAsync(claim.Id, request.Receipt, "receipt", 0);
                await _evidence.AddToClaimAsync(new ClaimEvidence { ClaimId = claim.Id, Type = "Receipt", FilePath = path });
            }

            var index = 0;
            foreach (var photo in (request.Photos ?? []).Take(MaxPhotos))
            {
                var path = await _storage.SaveClaimFileAsync(claim.Id, photo, "photo", index++);
                await _evidence.AddToClaimAsync(new ClaimEvidence { ClaimId = claim.Id, Type = "Photo", FilePath = path });
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        await _queue.EnqueueAsync(claim.Id);
        return Accepted(new { id = claim.Id, reference = Reference(claim), status = claim.Status.ToString() });
    }

    // Lista reklamacija prijavljenog korisnika.
    [HttpGet]
    public async Task<IActionResult> MyClaims()
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var claims = await _claims.GetByUserAsync(userId);
        var result = claims.Select(c => new ClaimSummaryResponse(
            c.Id,
            Reference(c),
            c.Purchase?.Product?.Name ?? "Nepoznat proizvod",
            c.Status.ToString(),
            c.SubmittedAt));

        return Ok(result);
    }

    // Detalji jedne reklamacije (samo vlasnik).
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var claim = await _claims.GetDetailAsync(id);
        if (claim is null || claim.UserId != userId)
        {
            return NotFound();
        }

        ClaimAnalysisResponse? analysis = null;
        string? explanation = null;
        if (!string.IsNullOrWhiteSpace(claim.AnalysisJson))
        {
            var snapshot = JsonSerializer.Deserialize<ClaimAnalysisSnapshot>(claim.AnalysisJson);
            if (snapshot is not null)
            {
                analysis = new ClaimAnalysisResponse(
                    snapshot.ReceiptCheck, snapshot.DamageCheck, snapshot.PolicyCheck, snapshot.RiskScore);
                explanation = snapshot.CustomerExplanation;
            }
        }

        return Ok(new ClaimDetailResponse(
            claim.Id,
            Reference(claim),
            claim.Purchase?.Product?.Name ?? "Nepoznat proizvod",
            claim.Status.ToString(),
            claim.SubmittedAt,
            claim.IssueType,
            claim.IssueDescription,
            explanation,
            analysis));
    }

    private string? CurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    private static string Reference(Claim claim) =>
        $"REK-{claim.SubmittedAt.Year}-{claim.Id:D5}";
}
