using System.Net.Http.Json;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Requests;
using Reklio.Api.DTOs.Responses;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T10.1 — prosljeđuje pitanje + istoriju Python chat endpointu. NE šalje korisničke podatke.
public class ChatService : IChatService
{
    private readonly HttpClient _http;

    public ChatService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            message = request.Message,
            history = request.History.Select(h => new { role = h.Role, content = h.Content }),
        };

        var response = await _http.PostAsJsonAsync("/chat", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ChatHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Prazan chat odgovor.");

        return new ChatResponse(dto.Answer, dto.CitedArticle);
    }
}
