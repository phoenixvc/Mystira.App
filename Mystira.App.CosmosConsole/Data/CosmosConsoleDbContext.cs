using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;

namespace Mystira.App.CosmosConsole.Data;

public class CosmosConsoleDbContext : DbContext
{
    public CosmosConsoleDbContext(DbContextOptions<CosmosConsoleDbContext> options)
        : base(options)
    {
    }

    // User and Profile Data
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<PendingSignup> PendingSignups { get; set; }

    // Scenario Management
    public DbSet<Scenario> Scenarios { get; set; }
    public DbSet<CharacterMap> CharacterMaps { get; set; }
    public DbSet<BadgeConfiguration> BadgeConfigurations { get; set; }

    // Game Session Management
    public DbSet<GameSession> GameSessions { get; set; }

    // Tracking and Analytics
    public DbSet<CompassTracking> CompassTrackings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure GameSession
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.AccountId);
        });

        // Configure Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Id);
        });

        // Configure Scenario
        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Id);
        });

        // Configure UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Name); // Keep using Name as primary key for backward compatibility
            entity.Property(e => e.Name).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Name);
        });
    }
}