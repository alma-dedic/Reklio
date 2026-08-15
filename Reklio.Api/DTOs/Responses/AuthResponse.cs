namespace Reklio.Api.DTOs.Responses;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshExpiresAt { get; set; }

    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
