namespace Reklio.Api.Services.Interfaces;

// Snima priložene slike (račun, fotografije oštećenja) na disk i vraća apsolutnu
// putanju koju pipeline kasnije čita.
public interface IFileStorageService
{
    Task<string> SaveClaimFileAsync(
        int claimId, IFormFile file, string kind, int index, CancellationToken cancellationToken = default);
}