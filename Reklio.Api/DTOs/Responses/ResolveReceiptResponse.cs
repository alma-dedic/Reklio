namespace Reklio.Api.DTOs.Responses;

// Jedna stavka računa ponuđena korisniku u dropdownu.
public record ResolveProductItem(int PurchaseId, string ProductName, string Category, decimal Price);

// Odgovor na resolve: proizvodi vezani za broj računa (iz slike ili ukucanog broja).
public record ResolveReceiptResponse(
    string? DocumentNumber,
    bool Found,
    IReadOnlyList<ResolveProductItem> Products);
