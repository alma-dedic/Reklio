using Microsoft.AspNetCore.Identity;
using Reklio.Api.Models;

namespace Reklio.Api.Data;

public static class DbSeeder
{
    // Test nalozi za razvoj i demo (T1.4).
    // Lozinke:
    //   operater@reklio.ba                              -> Operater123!
    //   kupac1@reklio.ba, kupac2@reklio.ba, kupac3@reklio.ba -> Kupac123!
    public static async Task SeedAsync(UserManager<User> userManager)
    {
        await EnsureUserAsync(userManager, "operater@reklio.ba", "Operater Test", UserRole.Operator, "Operater123!");
        await EnsureUserAsync(userManager, "kupac1@reklio.ba", "Kupac Test 1", UserRole.Customer, "Kupac123!");
        await EnsureUserAsync(userManager, "kupac2@reklio.ba", "Kupac Test 2", UserRole.Customer, "Kupac123!");
        await EnsureUserAsync(userManager, "kupac3@reklio.ba", "Kupac Test 3", UserRole.Customer, "Kupac123!");
    }

    private static async Task EnsureUserAsync(
        UserManager<User> userManager,
        string email,
        string fullName,
        UserRole role,
        string password)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            Role = role,
            RegisteredAt = DateTime.UtcNow
        };

        await userManager.CreateAsync(user, password);
    }
}