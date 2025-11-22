using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using YamlDotNet.Serialization;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for exporting badge configurations to YAML format
/// </summary>
public class ExportBadgeConfigurationUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<ExportBadgeConfigurationUseCase> _logger;

    public ExportBadgeConfigurationUseCase(
        IBadgeConfigurationRepository repository,
        ILogger<ExportBadgeConfigurationUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync()
    {
        var badgeConfigs = await _repository.GetAllAsync();
        var badgeConfigList = badgeConfigs.ToList();

        var badgeConfigYaml = new BadgeConfigurationYaml
        {
            Badges = badgeConfigList.Select(bc => new BadgeConfigurationYamlEntry
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

        var yaml = serializer.Serialize(badgeConfigYaml);

        _logger.LogInformation("Exported {Count} badge configurations to YAML", badgeConfigList.Count);
        return yaml;
    }
}

