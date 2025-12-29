using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data;

/// <summary>
/// PostgreSQL DbContext for polyglot persistence migration.
/// Contains only the entities that are candidates for PostgreSQL migration:
/// - Account (transactional, relational, FK target)
/// - GameSession (ACID required, frequent updates)
/// - PlayerScenarioScore (analytical queries, joins needed)
///
/// Per ADR-0013/0014 polyglot persistence strategy.
/// </summary>
public class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options)
        : base(options)
    {
    }

    // Migration candidate entities
    public DbSet<Account> Accounts { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<PlayerScenarioScore> PlayerScenarioScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAccount(modelBuilder);
        ConfigureGameSession(modelBuilder);
        ConfigurePlayerScenarioScore(modelBuilder);
    }

    private void ConfigureAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(36);

            entity.Property(e => e.ExternalUserId)
                .HasColumnName("external_user_id")
                .HasMaxLength(256);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(256);

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasMaxLength(50)
                .HasDefaultValue("Guest");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");

            // Store list as JSON array (PostgreSQL native JSON support)
            entity.Property(e => e.UserProfileIds)
                .HasColumnName("user_profile_ids")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(CreateListComparer<string>());

            entity.Property(e => e.CompletedScenarioIds)
                .HasColumnName("completed_scenario_ids")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(CreateListComparer<string>());

            // Owned entities stored as JSONB columns
            entity.OwnsOne(e => e.Subscription, subscription =>
            {
                subscription.ToJson("subscription");
            });

            entity.OwnsOne(e => e.Settings, settings =>
            {
                settings.ToJson("settings");
            });

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_accounts_email");
            entity.HasIndex(e => e.ExternalUserId).HasDatabaseName("ix_accounts_external_user_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_accounts_created_at");
        });
    }

    private void ConfigureGameSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.ToTable("game_sessions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(36);

            entity.Property(e => e.ScenarioId)
                .HasColumnName("scenario_id")
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.AccountId)
                .HasColumnName("account_id")
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.ProfileId)
                .HasColumnName("profile_id")
                .HasMaxLength(36);

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.CurrentSceneId)
                .HasColumnName("current_scene_id")
                .HasMaxLength(100);

            entity.Property(e => e.StartTime)
                .HasColumnName("start_time");

            entity.Property(e => e.EndTime)
                .HasColumnName("end_time");

            entity.Property(e => e.ElapsedTime)
                .HasColumnName("elapsed_time");

            entity.Property(e => e.IsPaused)
                .HasColumnName("is_paused")
                .HasDefaultValue(false);

            entity.Property(e => e.PausedAt)
                .HasColumnName("paused_at");

            entity.Property(e => e.SceneCount)
                .HasColumnName("scene_count")
                .HasDefaultValue(0);

            entity.Property(e => e.TargetAgeGroupName)
                .HasColumnName("target_age_group")
                .HasMaxLength(20);

            entity.Property(e => e.SelectedCharacterId)
                .HasColumnName("selected_character_id")
                .HasMaxLength(100);

            // Ignore computed property
            entity.Ignore(e => e.TargetAgeGroup);

            // Store list as JSON array
            entity.Property(e => e.PlayerNames)
                .HasColumnName("player_names")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(CreateListComparer<string>());

            // Complex nested structures as JSONB
            entity.Property(e => e.ChoiceHistory)
                .HasColumnName("choice_history")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SessionChoice>>(v, (JsonSerializerOptions?)null) ?? new List<SessionChoice>())
                .Metadata.SetValueComparer(CreateListComparer<SessionChoice>());

            entity.Property(e => e.EchoHistory)
                .HasColumnName("echo_history")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<EchoLog>>(v, (JsonSerializerOptions?)null) ?? new List<EchoLog>())
                .Metadata.SetValueComparer(CreateListComparer<EchoLog>());

            entity.Property(e => e.CompassValues)
                .HasColumnName("compass_values")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, CompassTracking>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, CompassTracking>())
                .Metadata.SetValueComparer(CreateDictionaryComparer<string, CompassTracking>());

            entity.Property(e => e.PlayerCompassProgressTotals)
                .HasColumnName("player_compass_progress_totals")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<PlayerCompassProgress>>(v, (JsonSerializerOptions?)null) ?? new List<PlayerCompassProgress>())
                .Metadata.SetValueComparer(CreateListComparer<PlayerCompassProgress>());

            entity.Property(e => e.Achievements)
                .HasColumnName("achievements")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SessionAchievement>>(v, (JsonSerializerOptions?)null) ?? new List<SessionAchievement>())
                .Metadata.SetValueComparer(CreateListComparer<SessionAchievement>());

            entity.Property(e => e.CharacterAssignments)
                .HasColumnName("character_assignments")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SessionCharacterAssignment>>(v, (JsonSerializerOptions?)null) ?? new List<SessionCharacterAssignment>())
                .Metadata.SetValueComparer(CreateListComparer<SessionCharacterAssignment>());

            // Indexes for common query patterns
            entity.HasIndex(e => e.AccountId).HasDatabaseName("ix_game_sessions_account_id");
            entity.HasIndex(e => e.ProfileId).HasDatabaseName("ix_game_sessions_profile_id");
            entity.HasIndex(e => e.ScenarioId).HasDatabaseName("ix_game_sessions_scenario_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_game_sessions_status");
            entity.HasIndex(e => e.StartTime).HasDatabaseName("ix_game_sessions_start_time");
            entity.HasIndex(e => new { e.AccountId, e.Status }).HasDatabaseName("ix_game_sessions_account_status");
        });
    }

    private void ConfigurePlayerScenarioScore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerScenarioScore>(entity =>
        {
            entity.ToTable("player_scenario_scores");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(36);

            entity.Property(e => e.ProfileId)
                .HasColumnName("profile_id")
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.ScenarioId)
                .HasColumnName("scenario_id")
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.GameSessionId)
                .HasColumnName("game_session_id")
                .HasMaxLength(36);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            // Store axis scores as JSONB for flexible querying
            entity.Property(e => e.AxisScores)
                .HasColumnName("axis_scores")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, float>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase))
                .Metadata.SetValueComparer(CreateDictionaryComparer<string, float>());

            // Indexes
            entity.HasIndex(e => e.ProfileId).HasDatabaseName("ix_player_scenario_scores_profile_id");
            entity.HasIndex(e => e.ScenarioId).HasDatabaseName("ix_player_scenario_scores_scenario_id");
            entity.HasIndex(e => new { e.ProfileId, e.ScenarioId })
                .IsUnique()
                .HasDatabaseName("uix_player_scenario_scores_profile_scenario");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_player_scenario_scores_created_at");
        });
    }

    #region Value Comparers

    private static ValueComparer<List<T>> CreateListComparer<T>()
    {
        return new ValueComparer<List<T>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c.ToList());
    }

    private static ValueComparer<Dictionary<TKey, TValue>> CreateDictionaryComparer<TKey, TValue>()
        where TKey : notnull
    {
        return new ValueComparer<Dictionary<TKey, TValue>>(
            (d1, d2) => d1 != null && d2 != null && d1.Count == d2.Count && !d1.Except(d2).Any(),
            d => d != null ? d.Aggregate(0, (a, kv) => HashCode.Combine(a, kv.Key.GetHashCode(), kv.Value != null ? kv.Value.GetHashCode() : 0)) : 0,
            d => d != null ? new Dictionary<TKey, TValue>(d) : new Dictionary<TKey, TValue>());
    }

    #endregion
}
