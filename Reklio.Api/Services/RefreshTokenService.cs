using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ReklioDbContext _db;
    private readonly JwtOptions _options;

    public RefreshTokenService(ReklioDbContext db, IOptions<JwtOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<RefreshToken> CreateAsync(string userId)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task<RefreshToken?> GetValidAsync(string token)
    {
        var now = DateTime.UtcNow;
        return await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.RevokedAt == null && t.ExpiresAt > now);
    }

    public async Task RevokeAsync(RefreshToken token)
    {
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
