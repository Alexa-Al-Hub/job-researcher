using System.Text.Json;
using JobAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace JobAgent.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SearchCriteria> SearchCriteria => Set<SearchCriteria>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<JobSkill> JobSkills => Set<JobSkill>();
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory => Set<ApplicationStatusHistory>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Platform).HasConversion<string>();
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Company).HasMaxLength(300);
            entity.Property(e => e.Url).HasMaxLength(2000);
            entity.HasIndex(e => e.Url).IsUnique();
            entity.HasOne(e => e.SearchCriteria)
                .WithMany(sc => sc.Jobs)
                .HasForeignKey(e => e.SearchCriteriaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(300);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<SearchCriteria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Keywords).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Platforms).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    c => c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
                    c => c.ToList()));
            entity.HasOne(e => e.User)
                .WithMany(u => u.SearchCriteria)
                .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SkillId });
            entity.HasOne(e => e.User).WithMany(u => u.UserSkills).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Skill).WithMany(s => s.UserSkills).HasForeignKey(e => e.SkillId);
        });

        modelBuilder.Entity<JobSkill>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.SkillId });
            entity.HasOne(e => e.Job).WithMany(j => j.JobSkills).HasForeignKey(e => e.JobId);
            entity.HasOne(e => e.Skill).WithMany(s => s.JobSkills).HasForeignKey(e => e.SkillId);
        });

        modelBuilder.Entity<Domain.Entities.Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Job).WithMany(j => j.Applications).HasForeignKey(e => e.JobId);
            entity.HasOne(e => e.User).WithMany(u => u.Applications).HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.JobId, e.UserId }).IsUnique();
        });

        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OldStatus).HasConversion<string>();
            entity.Property(e => e.NewStatus).HasConversion<string>();
            entity.HasOne(e => e.Application)
                .WithMany(a => a.StatusHistory)
                .HasForeignKey(e => e.ApplicationId);
        });
    }
}
