using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Reklio.Api.Models;

namespace Reklio.Api.Data;

public class ReklioDbContext : IdentityUserContext<User>
{
    public ReklioDbContext(DbContextOptions<ReklioDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }
}