namespace Reklio.Api.Models;

public class ClaimEvidence
{
    public int Id { get; set; }

    public int ClaimId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public Claim Claim { get; set; } = null!;
}