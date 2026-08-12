namespace Reklio.Api.DTOs.Requests;

public class ChatMessageDto
{
    public string Role { get; set; } = "user";   // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto> History { get; set; } = [];
}
