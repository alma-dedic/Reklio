using Microsoft.AspNetCore.Identity;

namespace Reklio.Api.Models;

public class User : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime RegisteredAt { get; set; }
}