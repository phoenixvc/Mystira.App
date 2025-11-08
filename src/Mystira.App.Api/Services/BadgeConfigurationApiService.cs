using Mystira.App.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Data;
using Mystira.App.Api.Models;
using YamlDotNet.Serialization;

namespace Mystira.App.Api.Services;

public class BadgeConfigurationApiService : IBadgeConfigurationApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<BadgeConfigurationApiService> _logger;

    public BadgeConfigurationApiService(MystiraAppDbContext context, ILogger<BadgeConfigurationApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BadgeConfiguration>> GetAllBadgeConfigurationsAsync()
    {
        var badgeConfigs = await _context.BadgeConfigurations.ToListAsync();
        
        // Initialize with default data if empty
        if (!badgeConfigs.Any())
        {
            await InitializeDefaultBadgeConfigurationsAsync();
            badgeConfigs = await _context.BadgeConfigurations.ToListAsync();
        }
        
        return badgeConfigs;
    }

    private async Task InitializeDefaultBadgeConfigurationsAsync()
    {
        var defaultBadges = new[]
        {
            new BadgeConfiguration
            {
                Id = "honesty_badge_1",
                Name = "Honesty",
                Message = "Congratulations, you have earned your first badge for being honest",
                Axis = "honesty",
                Threshold = 3.0f,
                ImageId = "media/images/badge_honesty_1.jpg"
            },
            new BadgeConfiguration
            {
                Id = "honesty_badge_2",
                Name = "Honesty",
                Message = "Congratulations, you have earned your second badge for displaying remarkable honesty",
                Axis = "honesty",
                Threshold = 7.0f,
                ImageId = "media/images/badge_honesty2.jpg"
            },
            new BadgeConfiguration
            {
                Id = "bravery_badge_1",
                Name = "Bravery",
                Message = "You've taken your first bold step—bravery badge earned!",
                Axis = "bravery",
                Threshold = 4.0f,
                ImageId = "media/images/badge_bravery_1.jpg"
            },
            new BadgeConfiguration
            {
                Id = "empathy_badge_1",
                Name = "Empathy",
                Message = "Your compassion shines through—first empathy badge unlocked",
                Axis = "empathy",
                Threshold = 2.5f,
                ImageId = "media/images/badge_empathy_1.jpg"
            },
            new BadgeConfiguration
            {
                Id = "trickery_badge_1",
                Name = "Trickery",
                Message = "Clever moves! You've earned your first trickery badge",
                Axis = "trickery",
                Threshold = 5.0f,
                ImageId = "media/images/badge_trickery_1.jpg"
            }
        };

        foreach (var badge in defaultBadges)
        {
            badge.CreatedAt = DateTime.UtcNow;
            badge.UpdatedAt = DateTime.UtcNow;
        }

        _context.BadgeConfigurations.AddRange(defaultBadges);
        await _context.SaveChangesAsync();
    }

    public async Task<BadgeConfiguration?> GetBadgeConfigurationAsync(string id)
    {
        return await _context.BadgeConfigurations.FirstOrDefaultAsync(bc => bc.Id == id);
    }

    public async Task<List<BadgeConfiguration>> GetBadgeConfigurationsByAxisAsync(string axis)
    {
        return await _context.BadgeConfigurations
            .Where(bc => bc.Axis.Equals(axis, StringComparison.OrdinalIgnoreCase))
            .OrderBy(bc => bc.Threshold)
            .ToListAsync();
    }

    public async Task<BadgeConfiguration> CreateBadgeConfigurationAsync(CreateBadgeConfigurationRequest request)
    {
        var existingBadgeConfig = await _context.BadgeConfigurations.FirstOrDefaultAsync(bc => bc.Id == request.Id);
        if (existingBadgeConfig != null)
        {
            throw new ArgumentException($"Badge configuration with ID {request.Id} already exists");
        }

        // Validate that the axis is from the master list
        if (!MasterLists.CompassAxes.Contains(request.Axis))
        {
            throw new ArgumentException($"Invalid compass axis: {request.Axis}. Must be one of: {string.Join(", ", MasterLists.CompassAxes)}");
        }

        var badgeConfig = new BadgeConfiguration
        {
            Id = request.Id,
            Name = request.Name,
            Message = request.Message,
            Axis = request.Axis,
            Threshold = request.Threshold,
            ImageId = request.ImageId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BadgeConfigurations.Add(badgeConfig);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created badge configuration {BadgeConfigId}", badgeConfig.Id);
        return badgeConfig;
    }

    public async Task<BadgeConfiguration?> UpdateBadgeConfigurationAsync(string id, UpdateBadgeConfigurationRequest request)
    {
        var badgeConfig = await _context.BadgeConfigurations.FirstOrDefaultAsync(bc => bc.Id == id);
        if (badgeConfig == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            badgeConfig.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            badgeConfig.Message = request.Message;
        }

        if (!string.IsNullOrWhiteSpace(request.Axis))
        {
            if (!MasterLists.CompassAxes.Contains(request.Axis))
            {
                throw new ArgumentException($"Invalid compass axis: {request.Axis}. Must be one of: {string.Join(", ", MasterLists.CompassAxes)}");
            }
            badgeConfig.Axis = request.Axis;
        }

        if (request.Threshold.HasValue)
        {
            badgeConfig.Threshold = request.Threshold.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.ImageId))
        {
            badgeConfig.ImageId = request.ImageId;
        }

        badgeConfig.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated badge configuration {BadgeConfigId}", id);
        return badgeConfig;
    }

    public async Task<bool> DeleteBadgeConfigurationAsync(string id)
    {
        var badgeConfig = await _context.BadgeConfigurations.FirstOrDefaultAsync(bc => bc.Id == id);
        if (badgeConfig == null)
        {
            return false;
        }

        _context.BadgeConfigurations.Remove(badgeConfig);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted badge configuration {BadgeConfigId}", id);
        return true;
    }

    public async Task<string> ExportBadgeConfigurationsAsYamlAsync()
    {
        var badgeConfigs = await _context.BadgeConfigurations.ToListAsync();

        var badgeConfigYaml = new BadgeConfigurationYaml
        {
            Badges = badgeConfigs.Select(bc => new BadgeConfigurationYamlEntry
            {
                Id = bc.Id,
                Name = bc.Name,
                Message = bc.Message,
                Axis = bc.Axis,
                Threshold = bc.Threshold,
                ImageId = bc.ImageId
            }).ToList()
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        return serializer.Serialize(badgeConfigYaml);
    }

    public async Task<List<BadgeConfiguration>> ImportBadgeConfigurationsFromYamlAsync(Stream yamlStream)
    {
        var deserializer = new DeserializerBuilder()
            .WithCaseInsensitivePropertyMatching()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(yamlStream);
        var yamlContent = await reader.ReadToEndAsync();

        var badgeConfigYaml = deserializer.Deserialize<BadgeConfigurationYaml>(yamlContent);
        
        var importedBadgeConfigs = new List<BadgeConfiguration>();

        foreach (var yamlEntry in badgeConfigYaml.Badges)
        {
            // Validate axis
            if (!MasterLists.CompassAxes.Contains(yamlEntry.Axis))
            {
                throw new ArgumentException($"Invalid compass axis in YAML: {yamlEntry.Axis}. Must be one of: {string.Join(", ", MasterLists.CompassAxes)}");
            }

            var badgeConfig = new BadgeConfiguration
            {
                Id = yamlEntry.Id,
                Name = yamlEntry.Name,
                Message = yamlEntry.Message,
                Axis = yamlEntry.Axis,
                Threshold = yamlEntry.Threshold,
                ImageId = yamlEntry.ImageId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Check if it exists and update or add
            var existing = await _context.BadgeConfigurations.FirstOrDefaultAsync(bc => bc.Id == badgeConfig.Id);
            if (existing != null)
            {
                _context.BadgeConfigurations.Remove(existing);
            }
            
            _context.BadgeConfigurations.Add(badgeConfig);
            importedBadgeConfigs.Add(badgeConfig);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Imported {Count} badge configurations from YAML", importedBadgeConfigs.Count);
        return importedBadgeConfigs;
    }
}