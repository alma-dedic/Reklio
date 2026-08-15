using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IPurchaseService
{
    Task<Purchase?> GetByIdAsync(int id);

    // Traži postojeću transakciju po prirodnom ključu (T2.2) — za dedup pri unosu.
    Task<Purchase?> FindByNaturalKeyAsync(string branch, string documentNumber, DateTime purchaseDate);

    Task<Purchase?> FindByDocumentNumberAsync(string documentNumber);

    // Sve stavke jednog računa (multi-stavka) — sa učitanim proizvodom, za dropdown izbor.
    Task<IReadOnlyList<Purchase>> FindAllByDocumentNumberAsync(string documentNumber);
}