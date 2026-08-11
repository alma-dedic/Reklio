namespace Reklio.Api.DTOs.Requests;

// Multipart form za podnošenje reklamacije (pravi tok, EPIC 4 dovršetak).
// Online: unosi se DocumentNumber (nema slike računa).
// InStore: prilaže se Receipt slika (broj se čita OCR-om u pipeline-u).
public class CreateClaimRequest
{
    public string PurchaseType { get; set; } = "InStore";   // "InStore" | "Online"

    public string? DocumentNumber { get; set; }             // obavezan za Online

    public string IssueType { get; set; } = string.Empty;

    public string IssueDescription { get; set; } = string.Empty;

    public IFormFile? Receipt { get; set; }                 // obavezan za InStore

    public List<IFormFile>? Photos { get; set; }
}