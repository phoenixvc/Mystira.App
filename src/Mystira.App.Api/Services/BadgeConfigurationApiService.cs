using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using YamlDotNet.Serialization;

namespace Mystira.App.Api.Services;

public class BadgeConfigurationApiService : IBadgeConfigurationApiService
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BadgeConfigurationApiService> _logger;

    public BadgeConfigurationApiService(
        IBadgeConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<BadgeConfigurationApiService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<BadgeConfiguration>> GetAllBadgeConfigurationsAsync()
    {
        var badgeConfigs = (await _repository.GetAllAsync()).ToList();

        // Initialize with default data if empty
        if (!badgeConfigs.Any())
        {
            await InitializeDefaultBadgeConfigurationsAsync();
            badgeConfigs = (await _repository.GetAllAsync()).ToList();
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
            await _repository.AddAsync(badge);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<BadgeConfiguration?> GetBadgeConfigurationAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<BadgeConfiguration>> GetBadgeConfigurationsByAxisAsync(string axis)
    {
        var configs = await _repository.GetByAxisAsync(axis);
        return configs
            .Where(bc => bc.Axis.Equals(axis, StringComparison.OrdinalIgnoreCase))
            .OrderBy(bc => bc.Threshold)
            .ToList();
    }

    public async Task<BadgeConfiguration> CreateBadgeConfigurationAsync(CreateBadgeConfigurationRequest request)
    {
        var existingBadgeConfig = await _repository.GetByIdAsync(request.Id);
        if (existingBadgeConfig != null)
        {
            throw new ArgumentException($"Badge configuration with ID {request.Id} already exists");
        }

        // Validate that the axis is from the master list
        if (CoreAxis.Parse(request.Axis) == null)
        {
            throw new ArgumentException($"Invalid compass axis: {request.Axis}. Must be one of: {string.Join(", ", GetAllCoreAxisNames())}");
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

        await _repository.AddAsync(badgeConfig);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created badge configuration {BadgeConfigId}", badgeConfig.Id);
        return badgeConfig;
    }

    public async Task<BadgeConfiguration?> UpdateBadgeConfigurationAsync(string id, UpdateBadgeConfigurationRequest request)
    {
        var badgeConfig = await _repository.GetByIdAsync(id);
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
            if (CoreAxis.Parse(request.Axis) == null)
            {
                throw new ArgumentException($"Invalid compass axis: {request.Axis}. Must be one of: {string.Join(", ", GetAllCoreAxisNames())}");
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
        await _repository.UpdateAsync(badgeConfig);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated badge configuration {BadgeConfigId}", id);
        return badgeConfig;
    }

    public async Task<bool> DeleteBadgeConfigurationAsync(string id)
    {
        var badgeConfig = await _repository.GetByIdAsync(id);
        if (badgeConfig == null)
        {
            return false;
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted badge configuration {BadgeConfigId}", id);
        return true;
    }

    public async Task<string> ExportBadgeConfigurationsAsYamlAsync()
    {
        var badgeConfigs = (await _repository.GetAllAsync()).ToList();

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
            if (CoreAxis.Parse(yamlEntry.Axis) == null)
            {
                throw new ArgumentException($"Invalid compass axis in YAML: {yamlEntry.Axis}. Must be one of: {string.Join(", ", GetAllCoreAxisNames())}");
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
            var existing = await _repository.GetByIdAsync(badgeConfig.Id);
            if (existing != null)
            {
                await _repository.DeleteAsync(existing.Id);
            }

            await _repository.AddAsync(badgeConfig);
            importedBadgeConfigs.Add(badgeConfig);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Imported {Count} badge configurations from YAML", importedBadgeConfigs.Count);
        return importedBadgeConfigs;
    }

    private static IEnumerable<string> GetAllCoreAxisNames()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Mystira.App.Domain", "Data", "CoreAxes.json");
        if (!File.Exists(filePath))
        {
            return Array.Empty<string>();
        }

        var json = File.ReadAllText(filePath);
        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }
}
