using Reklio.Api.DTOs.Requests;
using Reklio.Api.DTOs.Responses;

namespace Reklio.Api.Services.Interfaces;

// EPIC 10 — chatbot nad pravilnikom (RAG). Bez korisničkog konteksta.
public interface IChatService
{
    Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
