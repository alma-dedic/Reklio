namespace Reklio.Api.Models;

public class Purchase
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public PurchaseType PurchaseType { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    public decimal Amount { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}