namespace Reklio.Api.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    // Garantni rok u mjesecima po kategoriji (Elektronika=24, Audio i dodaci=12,
    // Potrošni dijelovi=6). Mašinski proračun garancije (decision gate, fn_features);
    // RAG pravilnik ima iste brojeve ali služi za LLM objašnjenje.
    public int WarrantyMonths { get; set; }

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}