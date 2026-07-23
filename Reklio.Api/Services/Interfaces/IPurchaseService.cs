using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IPurchaseService
{
    Task<Purchase?> GetByIdAsync(int id);

    // Traži postojeću transakciju po prirodnom ključu (T2.2) — za dedup pri unosu.
    Task<Purchase?> FindByNaturalKeyAsync(string branch, string documentNumber, DateTime purchaseDate);
}