using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

public class PurchaseService : IPurchaseService
{
    private readonly ReklioDbContext _db;

    public PurchaseService(ReklioDbContext db)
    {
        _db = db;
    }

    public async Task<Purchase?> GetByIdAsync(int id)
    {
        return await _db.Purchases.FindAsync(id);
    }

    public async Task<Purchase?> FindByNaturalKeyAsync(string branch, string documentNumber, DateTime purchaseDate)
    {
        return await _db.Purchases
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Branch == branch &&
                p.DocumentNumber == documentNumber &&
                p.PurchaseDate == purchaseDate);
    }

    public async Task<Purchase?> FindByDocumentNumberAsync(string documentNumber)
    {
        return await _db.Purchases
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.DocumentNumber == documentNumber);
    }
}