using JobAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobAgent.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs => Set<Job>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Platform).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Company).HasMaxLength(300);
            entity.Property(e => e.Url).HasMaxLength(2000);
            entity.HasIndex(e => e.Url).IsUnique();
            entity.HasIndex(e => e.Status);
        });
    }
}
