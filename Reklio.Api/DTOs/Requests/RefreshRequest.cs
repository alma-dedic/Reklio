namespace Reklio.Api.DTOs.Requests;

// Koristi se za /auth/refresh i /auth/logout.
public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
