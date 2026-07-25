using System.ComponentModel.DataAnnotations;

namespace Reklio.Api.DTOs.Requests;

public class CreateClaimRequest
{
    [Required]
    public int PurchaseId { get; set; }

    [Required]
    [MaxLength(100)]
    public string IssueType { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string IssueDescription { get; set; } = string.Empty;
}