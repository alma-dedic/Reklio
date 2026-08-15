namespace Reklio.Api.DTOs.Requests;

// Multipart form za podnošenje reklamacije.
// Proizvod se bira u wizardu (dropdown iz resolve-a) → šalje se PurchaseId.
// InStore i dalje prilaže Receipt sliku (za validaciju + operatera).
public class CreateClaimRequest
{
    public string PurchaseType { get; set; } = "InStore";   // "InStore" | "Online"

    public string? DocumentNumber { get; set; }             // referenca (razrješavanje ide preko PurchaseId)

    public int? PurchaseId { get; set; }                    // izabrana stavka računa; null = kupovina nije pronađena

    public string IssueType { get; set; } = string.Empty;

    public string IssueDescription { get; set; } = string.Empty;

    public IFormFile? Receipt { get; set; }                 // obavezan za InStore

    public List<IFormFile>? Photos { get; set; }
}