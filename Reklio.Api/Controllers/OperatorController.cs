using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Responses;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Reklio.Api.Controllers;

[ApiController]
[Route("api/operator")]
[Authorize(Roles = "Operator")]
public class OperatorController : ControllerBase
{
    private readonly IClaimService _claims;
    private readonly IClaimEvidenceService _evidence;
    private readonly INotificationService _notifications;

    public OperatorController(
        IClaimService claims,
        IClaimEvidenceService evidence,
        INotificationService notifications)
    {
        _claims = claims;
        _evidence = evidence;
        _notifications = notifications;
    }

    // Red eskaliranih, sortiran po riziku (najviši prvo).
    [HttpGet("claims")]
    public async Task<IActionResult> Queue()
    {
        var claims = await _claims.GetEscalatedAsync();
        var result = claims.Select(c => new OperatorClaimSummary(
            c.Id,
            Reference(c),
            c.Purchase?.Product?.Name ?? "Nepoznat proizvod",
            c.User?.FullName ?? "Nepoznat kupac",
            c.RiskScore,
            c.SubmittedAt));

        return Ok(result);
    }

    // Pun detalj za odluku.
    [HttpGet("claims/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var claim = await _claims.GetDetailAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        ClaimAnalysisResponse? analysis = null;
        string? operatorExplanation = null;
        IReadOnlyList<string> factors = [];
        if (!string.IsNullOrWhiteSpace(claim.AnalysisJson))
        {
            var snapshot = JsonSerializer.Deserialize<ClaimAnalysisSnapshot>(claim.AnalysisJson);
            if (snapshot is not null)
            {
                analysis = new ClaimAnalysisResponse(
                    snapshot.ReceiptCheck, snapshot.DamageCheck, snapshot.PolicyCheck, snapshot.RiskScore);
                operatorExplanation = snapshot.OperatorExplanation;
                factors = snapshot.Factors ?? [];
            }
        }

        var evidence = await _evidence.GetByClaimAsync(id);
        var evidenceItems = evidence.Select(e => new OperatorEvidenceItem(e.Id, e.Type)).ToList();

        return Ok(new OperatorClaimDetail(
            claim.Id,
            Reference(claim),
            claim.Purchase?.Product?.Name ?? "Nepoznat proizvod",
            claim.User?.FullName ?? "Nepoznat kupac",
            claim.User?.Email ?? string.Empty,
            claim.Status.ToString(),
            claim.SubmittedAt,
            claim.IssueType,
            claim.IssueDescription,
            analysis,
            operatorExplanation,
            factors,
            evidenceItems));
    }

    // Servira sliku dokaza (račun/fotografija) operateru.
    [HttpGet("evidence/{evidenceId:int}")]
    public async Task<IActionResult> Evidence(int evidenceId)
    {
        var evidence = await _evidence.GetByIdAsync(evidenceId);
        if (evidence is null || !System.IO.File.Exists(evidence.FilePath))
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(evidence.FilePath).ToLowerInvariant() == ".png"
            ? "image/png"
            : "image/jpeg";
        return PhysicalFile(evidence.FilePath, contentType);
    }

    [HttpPost("claims/{id:int}/approve")]
    public Task<IActionResult> Approve(int id) =>
        ResolveAsync(id, ClaimStatus.OperatorApproved, "odobrena");

    [HttpPost("claims/{id:int}/reject")]
    public Task<IActionResult> Reject(int id) =>
        ResolveAsync(id, ClaimStatus.OperatorRejected, "odbijena");

    private async Task<IActionResult> ResolveAsync(int id, ClaimStatus status, string verb)
    {
        var operatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (operatorId is null)
        {
            return Unauthorized();
        }

        var claim = await _claims.GetByIdAsync(id);
        if (claim is null)
        {
            return NotFound();
        }
        if (claim.Status != ClaimStatus.Escalated)
        {
            return BadRequest(new { message = "Reklamacija nije u statusu za operatersku odluku." });
        }

        await _claims.ResolveByOperatorAsync(id, operatorId, status);
        await _notifications.CreateAsync(new Notification
        {
            UserId = claim.UserId,
            ClaimId = id,
            Message = $"Vaša reklamacija {Reference(claim)} je {verb} nakon pregleda operatera.",
            IsRead = false,
        });

        return Ok(new { status = status.ToString() });
    }

    private static string Reference(Claim claim) =>
        $"REK-{claim.SubmittedAt.Year}-{claim.Id:D5}";
}
