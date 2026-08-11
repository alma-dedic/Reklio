namespace Reklio.Api.Models;

public class Claim
{
    public int Id { get; set; }

    // Podnosilac reklamacije.
    public string UserId { get; set; } = string.Empty;

    public int? PurchaseId { get; set; }

    // Operater koji je pregledao (null dok se ne eskalira/dodijeli).
    public string? OperatorId { get; set; }

    public ClaimStatus Status { get; set; }

    public string IssueType { get; set; } = string.Empty;

    public string IssueDescription { get; set; } = string.Empty;

    // Popunjava fraud model kasnije (EPIC 5).
    public double? RiskScore { get; set; }

    // Popunjava pipeline/operater kasnije.
    public string? AiExplanation { get; set; }

    // Snapshot AI nalaza za ekran detalja 
    public string? AnalysisJson { get; set; }

    public DateTime SubmittedAt { get; set; }

    public User User { get; set; } = null!;

    public User? Operator { get; set; }

    public Purchase? Purchase { get; set; }

    public ICollection<ClaimEvidence> Evidence { get; set; } = new List<ClaimEvidence>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}