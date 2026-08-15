namespace Reklio.Api.Models;

// Trajni, opozivi refresh token. Rotira se pri svakom osvježavanju (stari se poništava).
public class RefreshToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
