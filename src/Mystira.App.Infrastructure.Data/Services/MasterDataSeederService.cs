using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Services;

/// <summary>
/// Service for seeding master data (CompassAxes, Archetypes, EchoTypes, FantasyThemes, AgeGroups)
/// from JSON files into the database.
/// </summary>
public class MasterDataSeederService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MasterDataSeederService> _logger;

    public MasterDataSeederService(
        IServiceProvider serviceProvider,
        ILogger<MasterDataSeederService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAllAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MystiraAppDbContext>();

        await SeedCompassAxesAsync(context);
        await SeedArchetypesAsync(context);
        await SeedEchoTypesAsync(context);
        await SeedFantasyThemesAsync(context);
        await SeedAgeGroupsAsync(context);
    }

    private async Task SeedCompassAxesAsync(MystiraAppDbContext context)
    {
        if (await context.CompassAxes.AnyAsync())
        {
            _logger.LogInformation("CompassAxes already seeded, skipping");
            return;
        }

        var jsonPath = GetJsonFilePath("CoreAxes.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("CoreAxes.json not found at {Path}, skipping seeding", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var items = JsonSerializer.Deserialize<List<JsonValueItem>>(json, GetJsonOptions());

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No items found in CoreAxes.json");
            return;
        }

        var entities = items.Select(item => new CompassAxis
        {
            Id = GenerateDeterministicId("compass-axis", item.Value),
            Name = item.Value,
            Description = $"Compass axis: {item.Value}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.CompassAxes.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} compass axes", entities.Count);
    }

    private async Task SeedArchetypesAsync(MystiraAppDbContext context)
    {
        if (await context.ArchetypeDefinitions.AnyAsync())
        {
            _logger.LogInformation("Archetypes already seeded, skipping");
            return;
        }

        var jsonPath = GetJsonFilePath("Archetypes.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("Archetypes.json not found at {Path}, skipping seeding", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var items = JsonSerializer.Deserialize<List<JsonValueItem>>(json, GetJsonOptions());

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No items found in Archetypes.json");
            return;
        }

        var entities = items.Select(item => new ArchetypeDefinition
        {
            Id = GenerateDeterministicId("archetype", item.Value),
            Name = item.Value,
            Description = $"Archetype: {item.Value}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.ArchetypeDefinitions.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} archetypes", entities.Count);
    }

    private async Task SeedEchoTypesAsync(MystiraAppDbContext context)
    {
        if (await context.EchoTypeDefinitions.AnyAsync())
        {
            _logger.LogInformation("EchoTypes already seeded, skipping");
            return;
        }

        var jsonPath = GetJsonFilePath("EchoTypes.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("EchoTypes.json not found at {Path}, skipping seeding", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var items = JsonSerializer.Deserialize<List<JsonValueItem>>(json, GetJsonOptions());

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No items found in EchoTypes.json");
            return;
        }

        var entities = items.Select(item => new EchoTypeDefinition
        {
            Id = GenerateDeterministicId("echo-type", item.Value),
            Name = item.Value,
            Description = $"Echo type: {item.Value}",
            Category = GetEchoTypeCategory(item.Value),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.EchoTypeDefinitions.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} echo types", entities.Count);
    }

    private async Task SeedFantasyThemesAsync(MystiraAppDbContext context)
    {
        if (await context.FantasyThemeDefinitions.AnyAsync())
        {
            _logger.LogInformation("FantasyThemes already seeded, skipping");
            return;
        }

        var jsonPath = GetJsonFilePath("FantasyThemes.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("FantasyThemes.json not found at {Path}, skipping seeding", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var items = JsonSerializer.Deserialize<List<JsonValueItem>>(json, GetJsonOptions());

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No items found in FantasyThemes.json");
            return;
        }

        var entities = items.Select(item => new FantasyThemeDefinition
        {
            Id = GenerateDeterministicId("fantasy-theme", item.Value),
            Name = item.Value,
            Description = $"Fantasy theme: {item.Value}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.FantasyThemeDefinitions.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} fantasy themes", entities.Count);
    }

    private async Task SeedAgeGroupsAsync(MystiraAppDbContext context)
    {
        if (await context.AgeGroupDefinitions.AnyAsync())
        {
            _logger.LogInformation("AgeGroups already seeded, skipping");
            return;
        }

        var jsonPath = GetJsonFilePath("AgeGroups.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("AgeGroups.json not found at {Path}, skipping seeding", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var items = JsonSerializer.Deserialize<List<AgeGroupJsonItem>>(json, GetJsonOptions());

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No items found in AgeGroups.json");
            return;
        }

        var entities = items.Select(item => new AgeGroupDefinition
        {
            Id = GenerateDeterministicId("age-group", item.Value),
            Name = item.Name,
            Value = item.Value,
            MinimumAge = item.MinimumAge,
            MaximumAge = item.MaximumAge,
            Description = $"Age group for ages {item.MinimumAge}-{item.MaximumAge}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.AgeGroupDefinitions.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} age groups", entities.Count);
    }

    private static string GetJsonFilePath(string fileName)
    {
        // Look for the JSON file in the Domain/Data directory
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Try common paths relative to running directory
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Data", fileName),
            Path.Combine(currentDir, "Data", fileName),
            Path.Combine(currentDir, fileName),
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Return the most likely path even if it doesn't exist
        return Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Data", fileName));
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Generates a deterministic ID based on entity type and name.
    /// This ensures idempotent seeding operations.
    /// </summary>
    private static string GenerateDeterministicId(string entityType, string name)
    {
        var input = $"{entityType}:{name.ToLowerInvariant()}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        // Create a GUID-like format from the first 16 bytes of the hash
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes).ToString();
    }

    /// <summary>
    /// Categorizes echo types into logical groups for better organization.
    /// Categories: moral, emotional, behavioral, social, cognitive, meta
    /// </summary>
    private static string GetEchoTypeCategory(string echoType)
    {
        // Moral echo types - related to ethical choices and values
        var moralTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "honesty", "deception", "loyalty", "betrayal", "justice", "injustice",
            "fairness", "bias", "forgiveness", "revenge", "sacrifice", "selfishness",
            "obedience", "rebellion", "promise", "oath_made", "oath_broken",
            "lie_exposed", "secret_revealed", "first_blood"
        };

        // Emotional echo types - related to feelings and emotional states
        var emotionalTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "doubt", "confidence", "shame", "pride", "regret", "hope", "despair",
            "grief", "denial", "acceptance", "awakening", "resignation", "fear",
            "panic", "jealousy", "envy", "gratitude", "resentment", "love"
        };

        // Behavioral echo types - related to actions and conduct
        var behavioralTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "growth", "stagnation", "kindness", "neglect", "compassion", "coldness",
            "generosity", "bravery", "aggression", "cowardice", "protection",
            "avoidance", "confrontation", "flight", "freeze", "rescue",
            "denial_of_help", "risk_taking", "resilience"
        };

        // Social echo types - related to interactions and relationships
        var socialTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "trust", "manipulation", "support", "abandonment", "listening",
            "interrupting", "mockery", "encouragement", "humiliation", "respect",
            "disrespect", "sharing", "withholding", "blaming", "apologizing"
        };

        // Cognitive echo types - related to thinking and understanding
        var cognitiveTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "curiosity", "closed-mindedness", "truth_seeking", "value_conflict",
            "reflection", "projection", "mirroring", "internalization",
            "breakthrough", "denial_of_truth", "clarity", "lesson_learned",
            "lesson_ignored", "destiny_revealed"
        };

        // Identity echo types - related to self and persona
        var identityTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authenticity", "masking", "conformity", "individualism",
            "dependence", "independence", "attention_seeking", "withdrawal",
            "role_adoption", "role_rejection", "role_locked"
        };

        // Meta/System echo types - game mechanics and meta concepts
        var metaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pattern_repetition", "pattern_break", "echo_amplification",
            "influence_spread", "echo_collision", "legacy_creation",
            "reputation_change", "morality_shift", "alignment_pull", "world_change",
            "rule_checker", "what_if_scientist", "try_again_hero", "tidy_expert",
            "helper_captain_coop", "rhythm_explorer"
        };

        if (moralTypes.Contains(echoType)) return "moral";
        if (emotionalTypes.Contains(echoType)) return "emotional";
        if (behavioralTypes.Contains(echoType)) return "behavioral";
        if (socialTypes.Contains(echoType)) return "social";
        if (cognitiveTypes.Contains(echoType)) return "cognitive";
        if (identityTypes.Contains(echoType)) return "identity";
        if (metaTypes.Contains(echoType)) return "meta";

        return "other";
    }

    private class JsonValueItem
    {
        public string Value { get; set; } = string.Empty;
    }

    private class AgeGroupJsonItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int MinimumAge { get; set; }
        public int MaximumAge { get; set; }
    }
}
