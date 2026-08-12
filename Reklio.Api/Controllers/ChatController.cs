using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.DTOs.Requests;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chat;

    public ChatController(IChatService chat)
    {
        _chat = chat;
    }

    // Odgovara ISKLJUČIVO iz korpusa pravilnika — NE prosljeđuje nikakav korisnički/claim kontekst.
    [HttpPost]
    public async Task<IActionResult> Ask(ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Pitanje je prazno." });
        }

        return Ok(await _chat.AskAsync(request));
    }
}
