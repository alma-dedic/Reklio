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

    public AuthController(UserManager<User> userManager, IJwtTokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
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

        return StatusCode(StatusCodes.Status201Created, BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Pogresan email ili lozinka." });
        }

        return Ok(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString()
        };
    }
}