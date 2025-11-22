using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using YamlDotNet.Serialization;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for importing badge configurations from YAML format
/// </summary>
public class ImportBadgeConfigurationUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImportBadgeConfigurationUseCase> _logger;

    public ImportBadgeConfigurationUseCase(
        IBadgeConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ImportBadgeConfigurationUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<BadgeConfiguration>> ExecuteAsync(Stream yamlStream)
    {
        if (yamlStream == null)
        {
            throw new ArgumentNullException(nameof(yamlStream));
        }

        var deserializer = new DeserializerBuilder()
            .WithCaseInsensitivePropertyMatching()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(yamlStream);
        var yamlContent = await reader.ReadToEndAsync();

        var badgeConfigYaml = deserializer.Deserialize<BadgeConfigurationYaml>(yamlContent);
        if (badgeConfigYaml?.Badges == null)
        {
            throw new ArgumentException("Invalid YAML format: missing badges array");
        }

        var importedBadgeConfigs = new List<BadgeConfiguration>();

        foreach (var yamlEntry in badgeConfigYaml.Badges)
        {
            // Validate axis
            if (CoreAxis.Parse(yamlEntry.Axis) == null)
            {
                _logger.LogWarning("Skipping badge configuration {Id} with invalid axis: {Axis}", yamlEntry.Id, yamlEntry.Axis);
                continue;
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

            // Check if it exists and replace
            var existing = await _repository.GetByIdAsync(badgeConfig.Id);
            if (existing != null)
            {
                await _repository.DeleteAsync(badgeConfig.Id);
            }

            await _repository.AddAsync(badgeConfig);
            importedBadgeConfigs.Add(badgeConfig);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Imported {Count} badge configurations from YAML", importedBadgeConfigs.Count);
        return importedBadgeConfigs;
    }
}

