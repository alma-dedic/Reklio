using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.DTOs.Responses;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Reklio.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var items = await _notifications.GetByUserAsync(userId);
        return Ok(items.Select(n => new NotificationResponse(
            n.Id, n.Message, n.IsRead, n.ClaimId, Reference(n), n.CreatedAt)));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(new { count = await _notifications.GetUnreadCountAsync(userId) });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _notifications.MarkReadAsync(id, userId);
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _notifications.MarkAllReadAsync(userId);
        return Ok();
    }

    private string? CurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

    private static string Reference(Notification n) =>
        n.Claim is not null
            ? $"REK-{n.Claim.SubmittedAt.Year}-{n.ClaimId:D5}"
            : $"REK-{n.ClaimId:D5}";
}
