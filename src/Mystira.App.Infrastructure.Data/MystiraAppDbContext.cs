using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data;

/// <summary>
/// DbContext for Mystira App following Hexagonal Architecture
/// Located in Infrastructure.Data (outer layer) as per Ports and Adapters pattern
/// </summary>
public partial class MystiraAppDbContext : DbContext
{
    public MystiraAppDbContext(DbContextOptions<MystiraAppDbContext> options)
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
    public DbSet<ContentBundle> ContentBundles { get; set; }
    public DbSet<CharacterMap> CharacterMaps { get; set; }
    public DbSet<BadgeConfiguration> BadgeConfigurations { get; set; }

    // Media Management
    public DbSet<MediaAsset> MediaAssets { get; set; }
    public DbSet<MediaMetadataFile> MediaMetadataFiles { get; set; }
    public DbSet<CharacterMediaMetadataFile> CharacterMediaMetadataFiles { get; set; }
    public DbSet<CharacterMapFile> CharacterMapFiles { get; set; }
    public DbSet<AvatarConfigurationFile> AvatarConfigurationFiles { get; set; }

    // Game Session Management
    public DbSet<GameSession> GameSessions { get; set; }

    // Tracking and Analytics
    public DbSet<CompassTracking> CompassTrackings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Check if we're using in-memory database (for testing)
        var isInMemoryDatabase = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        // Configure UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Do not map computed value-object property AgeGroup; only persist AgeGroupName (string)
            entity.Ignore(e => e.AgeGroup);
            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("UserProfiles")
                      .HasPartitionKey("PartitionKeyId");
            }

            entity.Property(e => e.PreferredFantasyThemes)
                  .HasConversion(
                        v => string.Join(',', v.Select(e => e.Value)),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => FantasyTheme.Parse(s))
                            .Where(x => x != null)
                            .ToList()!)
                  .Metadata.SetValueComparer(new ValueComparer<List<FantasyTheme>>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToList()));

            // Configure UserBadge as owned by UserProfile
            entity.OwnsMany(p => p.EarnedBadges, badges =>
            {
                badges.WithOwner().HasForeignKey(b => b.UserProfileId);
                badges.HasKey(b => b.Id);

                // Only apply Cosmos DB specific configurations when not using in-memory database
                if (!isInMemoryDatabase)
                {
                    // No need for ToContainer or HasPartitionKey for owned entities
                    // They'll be embedded in the UserProfile document
                }

                badges.Property(b => b.UserProfileId).IsRequired();
                badges.Property(b => b.BadgeConfigurationId).IsRequired();
                badges.Property(b => b.BadgeName).IsRequired();
                badges.Property(b => b.BadgeMessage).IsRequired();
                badges.Property(b => b.Axis).IsRequired();

                // Indexes may not be applicable for owned entities in Cosmos DB
                // If using SQL Server, you can still configure these:
                if (isInMemoryDatabase)
                {
                    badges.HasIndex(b => b.UserProfileId);
                    badges.HasIndex(b => new { b.UserProfileId, b.BadgeConfigurationId }).IsUnique();
                }
            });
        });

        modelBuilder.Entity<UserProfile>().OwnsMany(p => p.EarnedBadges);

        // Configure Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("Accounts")
                      .HasPartitionKey("PartitionKeyId");
            }

            entity.Property(e => e.UserProfileIds)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                  .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToList()));

            entity.OwnsOne(e => e.Subscription, subscription =>
            {
                subscription.Property(s => s.PurchasedScenarios)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            });

            entity.OwnsOne(e => e.Settings);
        });

        // Configure ContentBundle
        modelBuilder.Entity<ContentBundle>(entity =>
        {
            entity.HasKey(e => e.Id);

            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("ContentBundles")
                      .HasPartitionKey("PartitionKeyId");
            }

            entity.Property(e => e.ScenarioIds)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                  .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToList()));

            entity.Property(e => e.Prices)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<BundlePrice>>(v, (JsonSerializerOptions?)null) ?? new List<BundlePrice>())
                  .Metadata.SetValueComparer(new ValueComparer<List<BundlePrice>>(
                      (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2).All(x => x.First.Value == x.Second.Value && x.First.Currency == x.Second.Currency),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Value.GetHashCode(), v.Currency.GetHashCode())),
                      c => c.Select(p => new BundlePrice { Value = p.Value, Currency = p.Currency }).ToList()));
        });

        // Configure CharacterMap
        modelBuilder.Entity<CharacterMap>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("CharacterMaps")
                      .HasPartitionKey("PartitionKeyId");
            }

            entity.OwnsOne(e => e.Metadata, metadata =>
            {
                metadata.Property(m => m.Traits)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            });
        });

        // Configure BadgeConfiguration
        modelBuilder.Entity<BadgeConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("BadgeConfigurations")
                      .HasPartitionKey("PartitionKeyId");
            }
        });

        // Configure Scenario
        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("Scenarios")
                      .HasPartitionKey("PartitionKeyId");
            }

            entity.Property(e => e.Tags)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToList()));

            entity.Property(e => e.Archetypes)
                  .HasConversion(
                        v => string.Join(',', v.Select(e => e.Value)),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => Archetype.Parse(s))
                            .Where(x => x != null)
                            .ToList()!)
                  .Metadata.SetValueComparer(new ValueComparer<List<Archetype>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

            entity.Property(e => e.CoreAxes)
                  .HasConversion(
                        v => string.Join(',', v.Select(e => e.Value)),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => CoreAxis.Parse(s))
                            .Where(x => x != null)
                            .ToList()!)
                  .Metadata.SetValueComparer(new ValueComparer<List<CoreAxis>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

            entity.OwnsMany(e => e.Characters, character =>
            {
                character.OwnsOne(c => c.Metadata, metadata =>
                {
                    metadata.Property(m => m.Role)
                            .HasConversion(
                                v => string.Join(',', v),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()));

                    metadata.Property(m => m.Archetype)
                            .HasConversion(
                                v => string.Join(',', v.Select(e => e.Value)),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => Archetype.Parse(s))
                                    .Where(x => x != null)
                                    .ToList()!)
                            .Metadata.SetValueComparer(new ValueComparer<List<Archetype>>(
                                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()));

                    metadata.Property(m => m.Traits)
                            .HasConversion(
                                v => string.Join(',', v),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()));
                });
            });

            entity.OwnsMany(e => e.Scenes, scene =>
            {
                scene.OwnsOne(s => s.Media);
                scene.OwnsMany(s => s.Branches, branch =>
                {
                    branch.OwnsOne(b => b.EchoLog, echoLog =>
                    {
                        echoLog.Property(e => e.EchoType)
                               .HasConversion(
                                   v => v.Value,
                                   v => EchoType.Parse(v) ?? EchoType.Parse("honesty")!);
                    });
                    branch.OwnsOne(b => b.CompassChange);
                });
                scene.OwnsMany(s => s.EchoReveals, reveal =>
                {
                    reveal.Property(r => r.EchoType)
                          .HasConversion(
                              v => v.Value,
                              v => EchoType.Parse(v) ?? EchoType.Parse("honesty")!);
                });
            });
        });

        // Configure GameSession
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.ToContainer("GameSessions")
                      .HasPartitionKey(e => e.AccountId);
            }

            entity.Property(e => e.PlayerNames)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToList()));

            entity.OwnsMany(e => e.ChoiceHistory, choice =>
            {
                choice.OwnsOne(c => c.EchoGenerated, echo =>
                {
                    echo.Property(e => e.EchoType)
                        .HasConversion(
                            v => v.Value,
                            v => EchoType.Parse(v) ?? EchoType.Parse("honesty")!);
                });
                choice.OwnsOne(c => c.CompassChange);
            });

            entity.OwnsMany(e => e.EchoHistory, echo =>
            {
                echo.Property(e => e.EchoType)
                    .HasConversion(
                        v => v.Value,
                        v => EchoType.Parse(v) ?? EchoType.Parse("honesty")!);
            });
            entity.OwnsMany(e => e.Achievements);

            // Configure CompassValues as a JSON property
            entity.Property(e => e.CompassValues)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, CompassTracking>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new()
                  )
                  .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, CompassTracking>>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                      c => new Dictionary<string, CompassTracking>(c)));
        });

        // Configure MediaAsset
        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.ToContainer("MediaAssets")
                      .HasPartitionKey(e => e.MediaType);
            }

            entity.Property(e => e.Tags)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  );

            entity.OwnsOne(e => e.Metadata, metadata =>
            {
                metadata.Property(m => m.AdditionalProperties)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>(),
                            new ValueComparer<Dictionary<string, object>>(
                                (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                                c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value != null ? v.Value.GetHashCode() : 0)) : 0,
                                c => c != null ? new Dictionary<string, object>(c) : new Dictionary<string, object>()
                            )
                        );
            });

            // Only apply indexes when using in-memory database (Cosmos DB doesn't support HasIndex)
            if (isInMemoryDatabase)
            {
                entity.HasIndex(e => e.MediaId).IsUnique();
                entity.HasIndex(e => e.MediaType);
                entity.HasIndex(e => e.CreatedAt);
            }
        });

        // Configure MediaAsset.Tags
        modelBuilder.Entity<MediaAsset>()
            .Property(m => m.Tags)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );

        // Configure MediaMetadataFile
        modelBuilder.Entity<MediaMetadataFile>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("MediaMetadataFiles")
                      .HasPartitionKey("PartitionKeyId");
            }

            // Use OwnsMany for proper JSON handling in Cosmos DB
            entity.OwnsMany(e => e.Entries, entry =>
            {
                entry.Property(e => e.ClassificationTags)
                    .HasConversion(new ClassificationTagListConverter())
                    .Metadata.SetValueComparer(new ValueComparer<List<ClassificationTag>>(
                        (c1, c2) => c1 != null && c2 != null &&
                                    c1.Count == c2.Count &&
                                    !c1.Except(c2, new ClassificationTagComparer()).Any(),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                        c => c.Select(x => new ClassificationTag { Key = x.Key, Value = x.Value }).ToList()
                    ));

                entry.Property(e => e.Modifiers)
                    .HasConversion(new ModifierListConverter())
                    .Metadata.SetValueComparer(new ValueComparer<List<Modifier>>(
                        (c1, c2) => c1 != null && c2 != null &&
                                    c1.Count == c2.Count &&
                                    !c1.Except(c2, new ModifierComparer()).Any(),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                        c => c.Select(x => new Modifier { Key = x.Key, Value = x.Value }).ToList()
                    ));
            });

        });

        // Configure CharacterMediaMetadataFile
        modelBuilder.Entity<CharacterMediaMetadataFile>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("CharacterMediaMetadataFiles")
                      .HasPartitionKey("PartitionKeyId");
            }

            // Use OwnsMany for proper JSON handling in Cosmos DB
            entity.OwnsMany(e => e.Entries, entry =>
            {
                entry.Property(e => e.Tags)
                     .HasConversion(
                         v => string.Join(',', v),
                         v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                     .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                         (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                         c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                         c => c.ToList()));
            });
        });

        // Configure CharacterMapFile
        modelBuilder.Entity<CharacterMapFile>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("CharacterMapFiles")
                      .HasPartitionKey("PartitionKeyId");
            }

            // Use OwnsMany for proper JSON handling in Cosmos DB
            // Note: Characters property uses CharacterMapFileCharacter from Domain
            entity.OwnsMany(e => e.Characters, character =>
            {
                character.OwnsOne(c => c.Metadata, metadata =>
                {
                    metadata.Property(m => m.Roles)
                            .HasConversion(
                                v => string.Join(',', v),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()));

                    metadata.Property(m => m.Archetypes)
                            .HasConversion(
                                v => string.Join(',', v),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()));

                    metadata.Property(m => m.Traits)
                            .HasConversion(
                                v => string.Join(',', v),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()));
                });
            });
        });

        // Configure AvatarConfigurationFile
        modelBuilder.Entity<AvatarConfigurationFile>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                entity.Property<string>("PartitionKeyId").ToJsonProperty("Id");
                entity.ToContainer("AvatarConfigurationFiles")
                      .HasPartitionKey("PartitionKeyId");
            }

            // Convert Dictionary<string, List<string>> for storage
            entity.Property(e => e.AgeGroupAvatars)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, List<string>>()
                  )
                  .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, List<string>>>(
                      (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count &&
                                  c1.Keys.All(k => c2.ContainsKey(k) && c1[k].SequenceEqual(c2[k])),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.Aggregate(0, (a2, s) => HashCode.Combine(a2, s.GetHashCode())))),
                      c => new Dictionary<string, List<string>>(c.ToDictionary(kvp => kvp.Key, kvp => new List<string>(kvp.Value)))));
        });

        // Configure PendingSignup
        modelBuilder.Entity<PendingSignup>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                // Map Email property to lowercase 'email' to match container partition key path /email
                entity.Property(e => e.Email).ToJsonProperty("email");

                // Note: PendingSignups container uses /email as partition key path
                entity.ToContainer("PendingSignups")
                      .HasPartitionKey(e => e.Email);
            }
        });

        // Configure CompassTracking as a separate container for analytics
        modelBuilder.Entity<CompassTracking>(entity =>
        {
            entity.HasKey(e => e.Axis);
            // Axis as key automatically maps to 'id' (lowercase) for document ID

            // Only apply Cosmos DB configurations when not using in-memory database
            if (!isInMemoryDatabase)
            {
                // Add shadow property that maps to 'Axis' (uppercase) in JSON for partition key
                entity.Property<string>("PartitionKeyAxis")
                      .ToJsonProperty("Axis");

                entity.ToContainer("CompassTrackings")
                      .HasPartitionKey("PartitionKeyAxis");
            }

            entity.OwnsMany(e => e.History);
        });
    }
}

public class ClassificationTagListConverter : ValueConverter<List<ClassificationTag>, string>
{
    public ClassificationTagListConverter()
        : base(
            // Convert to DB type (List<ClassificationTag> -> string)
            tags => ConvertToString(tags),
            // Convert from DB type (string -> List<ClassificationTag>)
            dbString => ConvertFromString(dbString))
    {
    }

    private static string ConvertToString(List<ClassificationTag> tags)
    {
        if (tags == null || !tags.Any())
        {
            return string.Empty;
        }

        return string.Join("|", tags.Select(tag => $"{tag.Key}:{tag.Value}"));
    }

    private static List<ClassificationTag> ConvertFromString(string dbString)
    {
        if (string.IsNullOrEmpty(dbString))
        {
            return new List<ClassificationTag>();
        }

        return dbString.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var parts = s.Split(':', 2);
                return new ClassificationTag
                {
                    Key = parts[0],
                    Value = parts.Length > 1 ? parts[1] : string.Empty
                };
            })
            .ToList();
    }
}

public class ClassificationTagComparer : IEqualityComparer<ClassificationTag>
{
    public bool Equals(ClassificationTag? x, ClassificationTag? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return x.Key == y.Key && x.Value == y.Value;
    }

    public int GetHashCode(ClassificationTag obj)
    {
        return HashCode.Combine(obj.Key, obj.Value);
    }
}

public class ModifierListConverter : ValueConverter<List<Modifier>, string>
{
    public ModifierListConverter()
        : base(
            // Convert to DB type (List<Modifier> -> string)
            modifiers => ConvertToString(modifiers),
            // Convert from DB type (string -> List<Modifier>)
            dbString => ConvertFromString(dbString))
    {
    }

    private static string ConvertToString(List<Modifier> modifiers)
    {
        if (modifiers == null || !modifiers.Any())
        {
            return string.Empty;
        }

        return string.Join("|", modifiers.Select(mod => $"{mod.Key}:{mod.Value}"));
    }

    private static List<Modifier> ConvertFromString(string dbString)
    {
        if (string.IsNullOrEmpty(dbString))
        {
            return new List<Modifier>();
        }

        return dbString.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var parts = s.Split(':', 2);
                return new Modifier
                {
                    Key = parts[0],
                    Value = parts.Length > 1 ? parts[1] : string.Empty
                };
            })
            .ToList();
    }
}

public class ModifierComparer : IEqualityComparer<Modifier>
{
    public bool Equals(Modifier? x, Modifier? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return x.Key == y.Key && x.Value == y.Value;
    }

    public int GetHashCode(Modifier obj)
    {
        return HashCode.Combine(obj.Key, obj.Value);
    }
}
