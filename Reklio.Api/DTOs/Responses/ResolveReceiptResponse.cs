namespace Reklio.Api.DTOs.Responses;

// Jedna stavka računa ponuđena korisniku u dropdownu.
public record ResolveProductItem(int PurchaseId, string ProductName, string Category, decimal Price);

// Odgovor na resolve. Status: "ok" (nađeno + validirano) | "mismatch" (iznos/datum) | "not_found".
public record ResolveReceiptResponse(
    string? DocumentNumber,
    string Status,
    IReadOnlyList<ResolveProductItem> Products);
