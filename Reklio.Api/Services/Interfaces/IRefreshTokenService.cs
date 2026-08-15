using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(string userId);

    // Vraća token ako je važeći (postoji, nije poništen, nije istekao), inače null.
    Task<RefreshToken?> GetValidAsync(string token);

    Task RevokeAsync(RefreshToken token);
}
