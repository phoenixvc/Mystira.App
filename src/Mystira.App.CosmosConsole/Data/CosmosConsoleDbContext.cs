using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mystira.App.Domain.Models;

namespace Mystira.App.CosmosConsole.Data;

public class CosmosConsoleDbContext : DbContext
{
    private readonly JsonSerializerOptions? _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

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

        // Ignore complex type, CompassValues, on GameSession
        modelBuilder
            .Entity<GameSession>()
            .Ignore(g => g.CompassValues);

        // Configure GameSession
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Id);
        });
        modelBuilder.Entity<GameSession>().ToContainer("GameSessions"); // Set container name to "GameSessions" ("GameSessions/" prefix will be added automatically)

        // Configure Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Id);
        });
        modelBuilder.Entity<Account>().ToContainer("Accounts"); // Set container name to "GameSessions" ("GameSessions/" prefix will be added automatically)

        // Configure Scenario
        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Id);
        });
        modelBuilder.Entity<Scenario>().ToContainer("Scenarios");

        // Configure UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Name); // Keep using Name as primary key for backward compatibility
            entity.Property(e => e.Name).ValueGeneratedNever();
            entity.HasPartitionKey(e => e.Name);
        });
        modelBuilder.Entity<UserProfile>().ToContainer("UserProfiles");

        // Configure CompassTracking
        modelBuilder.Entity<CompassTracking>().HasNoKey();
        modelBuilder.Entity<CompassTracking>().ToContainer("CompassTrackings");

        // Configure BadgeConfiguration
        modelBuilder.Entity<BadgeConfiguration>(entity =>
        {
            entity.HasPartitionKey(e => e.Id);
        });
        modelBuilder.Entity<BadgeConfiguration>().ToContainer("BadgeConfigurations");

        // Configure UserBadge
        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasPartitionKey(e => e.Id);
        });
        modelBuilder.Entity<UserBadge>().ToContainer("UserBadges");

        // Configure CharacterMap
        modelBuilder.Entity<CharacterMap>(entity =>
        {
            entity.HasPartitionKey(e => e.Id);
        });
        modelBuilder.Entity<CharacterMap>().ToContainer("CharacterMaps");

        // Configure PendingSignup
        modelBuilder.Entity<PendingSignup>(entity =>
        {
            entity.HasPartitionKey(e => e.Id);
        });

        // Configure ScenarioCharacter
        modelBuilder.Entity<ScenarioCharacter>(entity =>
        {
            entity.HasPartitionKey(e => e.Id);
        });

        // Configure List<string> properties that may come from comma-separated strings
        ConfigureListStringProperty<GameSession>(modelBuilder, e => e.PlayerNames);

        // Configure all List<string> properties on Scenario
        modelBuilder.Entity<Scenario>().Property(e => e.Archetypes).HasConversion(
            v => SerializeList(v.Select(e => e.Value).ToList()), 
            v => DeserializeList(v).Select(s => Archetype.Parse(s)).Where(x => x != null).ToList()!);
        modelBuilder.Entity<Scenario>().Property(e => e.CoreAxes).HasConversion(
            v => SerializeList(v.Select(e => e.Value).ToList()), 
            v => DeserializeList(v).Select(s => CoreAxis.Parse(s)).Where(x => x != null).ToList()!);
        ConfigureListStringProperty<Scenario>(modelBuilder, e => e.Tags);

        // Add configuration for the Character class
        modelBuilder.Entity<ScenarioCharacter>().OwnsOne(c => c.Metadata, metadata =>
        {
            // Configure the Archetype property if it should be a List<string>
            metadata.Property(m => m.Archetype)
                .HasConversion(
                    v => SerializeList(v.Select(e => e.Value).ToList()),
                    v => DeserializeList(v).Select(s => Archetype.Parse(s)).Where(x => x != null).ToList()!
                )
                .Metadata.SetValueComparer(new ValueComparer<List<Archetype>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));

            // Also configure any other properties in Metadata that might need conversion
            metadata.Property(m => m.Role)
                .HasConversion(
                    v => SerializeList(v),
                    v => DeserializeList(v)
                )
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));

            metadata.Property(m => m.Traits)
                .HasConversion(
                    v => SerializeList(v),
                    v => DeserializeList(v)
                )
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));
        });
    }

    private string DeserializeListRaw(string value)
    {
        // If it's a single value (not in JSON array format), we'll wrap it in an array
        if (!string.IsNullOrEmpty(value) && !value.StartsWith("[") && !value.EndsWith("]"))
        {
            // If it contains commas, it's likely already a comma-separated list
            if (value.Contains(","))
            {
                return $"[{string.Join(",", value.Split(',').Select(s => $"\"{s.Trim()}\""))}]";
            }
            else
            {
                return $"[\"{value}\"]";
            }
        }
        return value;
    }

    // Helper method for List<string> property configuration
    private void ConfigureListStringProperty<TEntity>(ModelBuilder modelBuilder,
        Expression<Func<TEntity, List<string>>> propertyExpression) where TEntity : class
    {
        modelBuilder.Entity<TEntity>()
            .Property(propertyExpression)
            .HasConversion(
                v => SerializeList(v),
                v => DeserializeList(v)
            )
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            ));
    }

    // Helpers for serialization/deserialization
    private string SerializeList(List<string> list)
    {
        if (list == null || list.Count == 0)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(list, _jsonOptions);
    }

    private List<string> DeserializeList(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
        }
        catch
        {
            // Handle legacy format (comma-separated string)
            if (json.Contains(","))
            {
                return json.Split(',').Where(s => s.Length > 0).ToList();
            }

            return json.Length > 0 ? new List<string> { json } : new List<string>();
        }
    }
}
