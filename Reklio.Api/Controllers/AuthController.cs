using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reklio.Api.DTOs.Requests;
using Reklio.Api.DTOs.Responses;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokens;

    public AuthController(
        UserManager<User> userManager,
        IJwtTokenService tokenService,
        IRefreshTokenService refreshTokens)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Conflict(new { message = "Korisnik sa ovim emailom vec postoji." });
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Role = UserRole.Customer,
            RegisteredAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Registracija ne loguje automatski — bez tokena, korisnik ide na login.
        return StatusCode(StatusCodes.Status201Created, new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            role = user.Role.ToString()
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Pogresan email ili lozinka." });
        }

        return Ok(await BuildAuthResponseAsync(user));
    }

    // Razmjena refresh tokena za novi par. Rotacija: stari se poništava.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var existing = await _refreshTokens.GetValidAsync(request.RefreshToken);
        if (existing is null)
        {
            return Unauthorized(new { message = "Nevalidan ili istekao refresh token." });
        }

        var user = await _userManager.FindByIdAsync(existing.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        await _refreshTokens.RevokeAsync(existing);
        return Ok(await BuildAuthResponseAsync(user));
    }

    // Odjava poništava refresh token server-side (dovoljno je posjedovanje tokena).
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var existing = await _refreshTokens.GetValidAsync(request.RefreshToken);
        if (existing is not null)
        {
            await _refreshTokens.RevokeAsync(existing);
        }

        return Ok();
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user);
        var refresh = await _refreshTokens.CreateAsync(user.Id);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            RefreshToken = refresh.Token,
            RefreshExpiresAt = refresh.ExpiresAt,
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString()
        };
    }
}
