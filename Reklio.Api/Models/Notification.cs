namespace Reklio.Api.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ClaimId { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public User User { get; set; } = null!;

    public Claim Claim { get; set; } = null!;
}