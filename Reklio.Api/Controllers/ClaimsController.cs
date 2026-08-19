using System.Text.Json;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.BackgroundJobs;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Requests;
using Reklio.Api.DTOs.Responses;
using Reklio.Api.DTOs.Validation;
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
    private readonly IOcrService _ocr;
    private readonly IReceiptValidationService _validation;

    public ClaimsController(
        IClaimService claims,
        IPurchaseService purchases,
        IClaimEvidenceService evidence,
        IFileStorageService storage,
        IClaimQueue queue,
        IOcrService ocr,
        IReceiptValidationService validation)
    {
        _claims = claims;
        _purchases = purchases;
        _evidence = evidence;
        _storage = storage;
        _queue = queue;
        _ocr = ocr;
        _validation = validation;
    }

    // In-store: JEDINI OCR poziv. Pročita račun i odmah validira (broj + iznos + datum).
    // Pipeline poslije NE radi OCR ponovo — vjeruje ovoj validaciji.
    [HttpPost("resolve-receipt")]
    public async Task<IActionResult> ResolveReceipt(IFormFile receipt, CancellationToken ct)
    {
        if (receipt is null || receipt.Length == 0)
        {
            return BadRequest(new { message = "Slika računa je obavezna." });
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"reklio-ocr-{Guid.NewGuid():N}.img");
        try
        {
            await using (var fs = System.IO.File.Create(tempPath))
            {
                await receipt.CopyToAsync(fs, ct);
            }

            var ocr = await _ocr.ExtractReceiptAsync(tempPath, ct);
            var validation = await _validation.ValidateDocumentAsync(ocr);

            var products = validation.Status == ReceiptValidationStatus.Valid
                ? await ProductsForAsync(ocr.DocumentNumber)
                : [];

            return Ok(new ResolveReceiptResponse(ocr.DocumentNumber, StatusText(validation.Status), products));
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }

    // Online: lookup po ručno ukucanom broju (nema slike → nema OCR-a ni iznos/datum provjere).
    [HttpGet("resolve-purchase")]
    public async Task<IActionResult> ResolvePurchase([FromQuery] string documentNumber)
    {
        var products = await ProductsForAsync(documentNumber);
        var status = products.Count > 0 ? "ok" : "not_found";
        return Ok(new ResolveReceiptResponse(documentNumber?.Trim(), status, products));
    }

    private async Task<List<ResolveProductItem>> ProductsForAsync(string? documentNumber)
    {
        var trimmed = documentNumber?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        var lines = await _purchases.FindAllByDocumentNumberAsync(trimmed);
        return lines
            .Select(p => new ResolveProductItem(p.Id, p.Product.Name, p.Product.Category, p.Amount))
            .ToList();
    }

    private static string StatusText(ReceiptValidationStatus status) => status switch
    {
        ReceiptValidationStatus.Valid => "ok",
        ReceiptValidationStatus.Mismatch => "mismatch",
        _ => "not_found",
    };

    // T3.2 — vrati 202 odmah, obradu prepusti redu čekanja.
    // Proizvod je već izabran u wizardu (PurchaseId). Ako je null (kupovina nije pronađena),
    // pipeline to tretira kao PURCHASE_NOT_FOUND i odbija.
    [HttpPost]
    public async Task<IActionResult> Submit([FromForm] CreateClaimRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isOnline = string.Equals(request.PurchaseType, "Online", StringComparison.OrdinalIgnoreCase);
        if (!isOnline && request.Receipt is null)
        {
            return BadRequest(new { message = "Slika računa je obavezna za fizičku kupovinu." });
        }

        // Proizvod (razriješena kupovina) je obavezan — reklamacija bez nje se ne prima.
        if (request.PurchaseId is not int selectedId
            || await _purchases.GetByIdAsync(selectedId) is null)
        {
            return BadRequest(new { message = "Izaberite proizvod — kupovina nije razriješena." });
        }

        var claim = new Claim
        {
            UserId = userId,
            PurchaseId = selectedId,
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
                // Rizik prevare je interni (za operatera) — kupcu se NE šalje.
                analysis = new ClaimAnalysisResponse(
                    snapshot.ReceiptCheck, snapshot.DamageCheck, snapshot.PolicyCheck, null);
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
