using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using YamlDotNet.Serialization;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for exporting character maps to YAML format
/// </summary>
public class ExportCharacterMapUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly ILogger<ExportCharacterMapUseCase> _logger;

    public ExportCharacterMapUseCase(
        ICharacterMapRepository repository,
        ILogger<ExportCharacterMapUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync()
    {
        var characterMaps = await _repository.GetAllAsync();
        var characterMapList = characterMaps.ToList();

        var characterMapYaml = new CharacterMapYaml
        {
            Characters = characterMapList.Select(cm => new CharacterMapYamlEntry
            {
                Id = cm.Id,
                Name = cm.Name,
                Image = cm.Image,
                Audio = cm.Audio,
                Metadata = cm.Metadata
            }).ToList()
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(characterMapYaml);

        _logger.LogInformation("Exported {Count} character maps to YAML", characterMapList.Count);
        return yaml;
    }
}

