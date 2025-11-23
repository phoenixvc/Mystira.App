using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Data;
using YamlDotNet.Serialization;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for importing character maps from YAML format
/// </summary>
public class ImportCharacterMapUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImportCharacterMapUseCase> _logger;

    public ImportCharacterMapUseCase(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ImportCharacterMapUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<CharacterMap>> ExecuteAsync(Stream yamlStream)
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

        var characterMapYaml = deserializer.Deserialize<CharacterMapYaml>(yamlContent);
        if (characterMapYaml?.Characters == null)
        {
            throw new ArgumentException("Invalid YAML format: missing characters array");
        }

        var importedCharacterMaps = new List<CharacterMap>();

        foreach (var yamlEntry in characterMapYaml.Characters)
        {
            var characterMap = new CharacterMap
            {
                Id = yamlEntry.Id,
                Name = yamlEntry.Name,
                Image = yamlEntry.Image,
                Audio = yamlEntry.Audio,
                Metadata = yamlEntry.Metadata ?? new CharacterMetadata(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Check if it exists and replace
            var existing = await _repository.GetByIdAsync(characterMap.Id);
            if (existing != null)
            {
                await _repository.DeleteAsync(characterMap.Id);
            }

            await _repository.AddAsync(characterMap);
            importedCharacterMaps.Add(characterMap);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Imported {Count} character maps from YAML", importedCharacterMaps.Count);
        return importedCharacterMaps;
    }
}

