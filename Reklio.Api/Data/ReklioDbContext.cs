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

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimEvidence> ClaimEvidence => Set<ClaimEvidence>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        builder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Category).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasData(CatalogSeed.Products);
        });

        builder.Entity<Purchase>(entity =>
        {
            entity.Property(p => p.PurchaseType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(p => p.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Branch).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Amount).HasPrecision(18, 2);

            // T2.2 — prirodni ključ: jedna stavka računa = jedan red. Isti račun (broj+datum+
            // poslovnica) može imati više proizvoda, ali ne isti proizvod dvaput.
            entity.HasIndex(p => new { p.Branch, p.DocumentNumber, p.PurchaseDate, p.ProductId }).IsUnique();

            entity.HasOne(p => p.Product)
                .WithMany(p => p.Purchases)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Claim>(entity =>
        {
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(c => c.IssueType).HasMaxLength(100).IsRequired();
            entity.Property(c => c.IssueDescription).HasMaxLength(2000).IsRequired();

            // T2.3 — dvije FK ka User: obje Restrict (inače SQL Server baca 1785
            // zbog dvije cascade putanje ka istoj tabeli).
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Operator)
                .WithMany()
                .HasForeignKey(c => c.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Purchase)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.PurchaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClaimEvidence>(entity =>
        {
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Claim)
                .WithMany(c => c.Evidence)
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(t => t.Token).HasMaxLength(200).IsRequired();
            entity.HasIndex(t => t.Token).IsUnique();

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Claim)
                .WithMany(c => c.Notifications)
                .HasForeignKey(n => n.ClaimId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}