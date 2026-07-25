using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.BackgroundJobs;
using Reklio.Api.DTOs.Requests;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claims;
    private readonly IPurchaseService _purchases;
    private readonly IClaimQueue _queue;

    public ClaimsController(IClaimService claims, IPurchaseService purchases, IClaimQueue queue)
    {
        _claims = claims;
        _purchases = purchases;
        _queue = queue;
    }

    // T3.2 — vrati 202 odmah, obradu prepusti redu čekanja.
    [HttpPost]
    public async Task<IActionResult> Submit(CreateClaimRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var purchase = await _purchases.GetByIdAsync(request.PurchaseId);
        if (purchase is null)
        {
            return NotFound(new { message = "Kupovina ne postoji." });
        }

        var claim = new Claim
        {
            UserId = userId,
            PurchaseId = request.PurchaseId,
            IssueType = request.IssueType,
            IssueDescription = request.IssueDescription,
            Status = ClaimStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        await _claims.CreateAsync(claim);
        await _queue.EnqueueAsync(claim.Id);

        return Accepted(new { id = claim.Id, status = claim.Status.ToString() });
    }
}