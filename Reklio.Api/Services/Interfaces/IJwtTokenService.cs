using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}