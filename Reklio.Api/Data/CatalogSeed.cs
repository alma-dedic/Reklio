using Reklio.Api.Models;

namespace Reklio.Api.Data;

// Fiksni katalog TehnoDom (T2.4). Referenca za simulator (E5), RAG korpus (E7)
// i hero račune (E13). WarrantyMonths ide po kategoriji:
// Elektronika=24, Audio i dodaci=12, Potrošni dijelovi=6.
public static class CatalogSeed
{
    public static readonly Product[] Products =
    [
        new() { Id = 1, Name = "Pametni telefon X20", Category = "Elektronika", Price = 899m, WarrantyMonths = 24 },
        new() { Id = 2, Name = "Laptop Pro 15", Category = "Elektronika", Price = 1890m, WarrantyMonths = 24 },
        new() { Id = 3, Name = "Tableta 10.1\"", Category = "Elektronika", Price = 549m, WarrantyMonths = 24 },
        new() { Id = 4, Name = "Kamera GX10", Category = "Elektronika", Price = 459m, WarrantyMonths = 24 },

        new() { Id = 5, Name = "Bežične slušalice Q3", Category = "Audio i dodaci", Price = 149m, WarrantyMonths = 12 },
        new() { Id = 6, Name = "Zvučnik prijenosni", Category = "Audio i dodaci", Price = 129m, WarrantyMonths = 12 },
        new() { Id = 7, Name = "USB-C kabl 1m", Category = "Audio i dodaci", Price = 19.90m, WarrantyMonths = 12 },
        new() { Id = 8, Name = "Punjač brzi 30W", Category = "Audio i dodaci", Price = 39m, WarrantyMonths = 12 },

        new() { Id = 9, Name = "Powerbank 10000mAh", Category = "Potrošni dijelovi", Price = 79m, WarrantyMonths = 6 },
        new() { Id = 10, Name = "Baterija za laptop (zamjenska)", Category = "Potrošni dijelovi", Price = 99m, WarrantyMonths = 6 },
    ];
}