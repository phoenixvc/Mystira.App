using System.Text.Json;
using Json.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Services;

public class BadgeConfigurationLoaderService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BadgeConfigurationLoaderService> _logger;
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public BadgeConfigurationLoaderService(
        IServiceProvider serviceProvider,
        ILogger<BadgeConfigurationLoaderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task LoadAndSeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MystiraAppDbContext>();

        if (await context.AxisAchievements.AnyAsync() || await context.Badges.AnyAsync())
        {
            _logger.LogInformation("Badge configuration already seeded, skipping");
            return;
        }

        var schema = await LoadSchemaAsync();
        if (schema == null)
        {
            _logger.LogWarning("Badge configuration schema not found, skipping badge seeding");
            return;
        }

        var ageGroups = new[] { "1-2", "3-5", "6-9", "10-12", "13-18" };
        foreach (var ageGroup in ageGroups)
        {
            await LoadAndSeedBadgeConfigurationAsync(context, schema, ageGroup);
        }
    }

    private async Task<JsonSchema?> LoadSchemaAsync()
    {
        var schemaPath = GetSchemaFilePath();
        if (!File.Exists(schemaPath))
        {
            _logger.LogWarning("Schema file not found at {Path}", schemaPath);
            return null;
        }

        try
        {
            var schemaJson = await File.ReadAllTextAsync(schemaPath);
            return JsonSchema.FromText(schemaJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load badge configuration schema from {Path}", schemaPath);
            return null;
        }
    }

    private async Task LoadAndSeedBadgeConfigurationAsync(MystiraAppDbContext context, JsonSchema schema, string ageGroupId)
    {
        var configPath = GetBadgeConfigFilePath(ageGroupId);
        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Badge configuration file not found for age group {AgeGroup} at {Path}", ageGroupId, configPath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var jsonDoc = JsonDocument.Parse(json);

            var validationResults = schema.Evaluate(jsonDoc.RootElement);
            if (!validationResults.IsValid)
            {
                _logger.LogError("Badge configuration for age group {AgeGroup} failed schema validation: {Errors}",
                    ageGroupId, string.Join(", ", validationResults.Errors?.Select(e => e.Value) ?? Array.Empty<string>()));
                return;
            }

            var config = JsonSerializer.Deserialize<BadgeConfigurationJson>(json, GetJsonOptions());
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize badge configuration for age group {AgeGroup}", ageGroupId);
                return;
            }

            await SeedAxisAchievementsAsync(context, config);
            await SeedBadgesAsync(context, config);

            _logger.LogInformation("Loaded badge configuration for age group {AgeGroup}: {AxisCount} axis achievements, {BadgeCount} badges",
                ageGroupId, config.AxisAchievements.Count, config.Badges.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load badge configuration for age group {AgeGroup}", ageGroupId);
        }
    }

    private async Task SeedAxisAchievementsAsync(MystiraAppDbContext context, BadgeConfigurationJson config)
    {
        foreach (var achievement in config.AxisAchievements)
        {
            var entity = new AxisAchievement
            {
                Id = GenerateDeterministicId("axis-achievement", config.AgeGroupId, achievement.CompassAxisId, achievement.AxesDirection),
                AgeGroupId = config.AgeGroupId,
                CompassAxisId = achievement.CompassAxisId,
                AxesDirection = achievement.AxesDirection,
                Description = achievement.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.AxisAchievements.AddAsync(entity);
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedBadgesAsync(MystiraAppDbContext context, BadgeConfigurationJson config)
    {
        foreach (var badge in config.Badges)
        {
            var entity = new Badge
            {
                Id = GenerateDeterministicId("badge", config.AgeGroupId, badge.CompassAxisId, badge.Tier, badge.TierOrder.ToString()),
                AgeGroupId = config.AgeGroupId,
                CompassAxisId = badge.CompassAxisId,
                Tier = badge.Tier,
                TierOrder = badge.TierOrder,
                Title = badge.Title,
                Description = badge.Description,
                RequiredScore = badge.RequiredScore,
                ImageId = badge.ImageId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Badges.AddAsync(entity);
        }

        await context.SaveChangesAsync();
    }

    private static string GetSchemaFilePath()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var schemaDir = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Schemas"));

        var possiblePaths = new[]
        {
            Path.Combine(schemaDir, "badge-configuration.schema.json"),
            Path.Combine(currentDir, "Schemas", "badge-configuration.schema.json"),
            Path.Combine(currentDir, "badge-configuration.schema.json")
        };

        return possiblePaths.Select(Path.GetFullPath).FirstOrDefault(File.Exists) 
               ?? Path.Combine(schemaDir, "badge-configuration.schema.json");
    }

    private static string GetBadgeConfigFilePath(string ageGroupId)
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var badgesDir = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Data", "Badges"));

        var possiblePaths = new[]
        {
            Path.Combine(badgesDir, $"{ageGroupId}.json"),
            Path.Combine(currentDir, "Data", "Badges", $"{ageGroupId}.json"),
            Path.Combine(currentDir, "Badges", $"{ageGroupId}.json")
        };

        return possiblePaths.Select(Path.GetFullPath).FirstOrDefault(File.Exists) 
               ?? Path.Combine(badgesDir, $"{ageGroupId}.json");
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private static string GenerateDeterministicId(string entityType, params string[] parts)
    {
        var input = $"{entityType}:{string.Join(":", parts.Select(p => p.ToLowerInvariant()))}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes).ToString();
    }

    private class BadgeConfigurationJson
    {
        public string AgeGroupId { get; set; } = string.Empty;
        public List<AxisAchievementJson> AxisAchievements { get; set; } = new();
        public List<BadgeJson> Badges { get; set; } = new();
    }

    private class AxisAchievementJson
    {
        public string CompassAxisId { get; set; } = string.Empty;
        public string AxesDirection { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private class BadgeJson
    {
        public string Tier { get; set; } = string.Empty;
        public int TierOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float RequiredScore { get; set; }
        public string CompassAxisId { get; set; } = string.Empty;
        public string ImageId { get; set; } = string.Empty;
    }
}
