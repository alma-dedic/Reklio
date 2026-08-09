using System.Text.Json.Serialization;

namespace Reklio.Api.DTOs.Ai;

// Odgovor Python OCR endpointa (POST /analyze/receipt).
public class OcrHttpResponse
{
    [JsonPropertyName("document_number")]
    public string? DocumentNumber { get; set; }

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyName("purchase_date")]
    public string? PurchaseDate { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("raw_text")]
    public string? RawText { get; set; }
}
