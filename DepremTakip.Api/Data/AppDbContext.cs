using DepremTakip.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DepremTakip.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Earthquake> Earthquakes => Set<Earthquake>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Earthquake>(e =>
        {
            e.Property(x => x.EventId).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique();   // Aynı deprem iki kez eklenemesin

            e.Property(x => x.MagnitudeType).HasMaxLength(8);
            e.Property(x => x.Location).HasMaxLength(256);
            e.Property(x => x.Province).HasMaxLength(64);
            e.Property(x => x.District).HasMaxLength(64);

            // Filtrelerde sık kullanacağımız kolonlar, sorgular hızlı olsun diye index
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => x.Magnitude);
            e.HasIndex(x => x.Province);
        });
    }
}