using Mystira.App.Contracts.Requests.CharacterMaps;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Data;
using YamlDotNet.Serialization;

namespace Mystira.App.Api.Services;

public class CharacterMapApiService : ICharacterMapApiService
{
    private readonly ICharacterMapRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CharacterMapApiService> _logger;

    public CharacterMapApiService(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CharacterMapApiService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<CharacterMap>> GetAllCharacterMapsAsync()
    {
        var characterMaps = (await _repository.GetAllAsync()).ToList();

        // Initialize with default data if empty
        if (!characterMaps.Any())
        {
            await InitializeDefaultCharacterMapsAsync();
            characterMaps = (await _repository.GetAllAsync()).ToList();
        }

        return characterMaps;
    }

    private async Task InitializeDefaultCharacterMapsAsync()
    {
        var elarion = new CharacterMap
        {
            Id = "elarion",
            Name = "Elarion the Wise",
            Image = "media/images/elarion.jpg",
            Audio = "media/audio/elarion_voice.mp3",
            Metadata = new App.Domain.Models.CharacterMetadata
            {
                Roles = ["mentor", "peacemaker"],
                Archetypes = ["guardian", "quiet strength"],
                Species = "elf",
                Age = 312,
                Traits = ["wise", "calm", "mysterious"],
                Backstory = "A sage from the Verdant Isles who guides lost heroes."
            }
        };

        var grubb = new CharacterMap
        {
            Id = "grubb",
            Name = "Grubb the Goblin",
            Image = "media/images/grubb.png",
            Audio = "media/audio/grubb_laugh.mp3",
            Metadata = new App.Domain.Models.CharacterMetadata
            {
                Roles = ["trickster", "sly"],
                Archetypes = ["sneaky foe"],
                Species = "goblin",
                Age = 14,
                Traits = ["sneaky", "funny", "chaotic"],
                Backstory = "An outcast goblin who joins adventurers for laughs and loot."
            }
        };

        await _repository.AddAsync(elarion);
        await _repository.AddAsync(grubb);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CharacterMap?> GetCharacterMapAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<CharacterMap> CreateCharacterMapAsync(CreateCharacterMapRequest request)
    {
        var existingCharacterMap = await _repository.GetByIdAsync(request.Id);
        if (existingCharacterMap != null)
        {
            throw new ArgumentException($"Character map with ID {request.Id} already exists");
        }

        var characterMap = new CharacterMap
        {
            Id = request.Id,
            Name = request.Name,
            Image = request.Image,
            Audio = request.Audio,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(characterMap);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created character map {CharacterMapId}", characterMap.Id);
        return characterMap;
    }

    public async Task<CharacterMap?> UpdateCharacterMapAsync(string id, UpdateCharacterMapRequest request)
    {
        var characterMap = await _repository.GetByIdAsync(id);
        if (characterMap == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            characterMap.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Image))
        {
            characterMap.Image = request.Image;
        }

        if (request.Audio != null)
        {
            characterMap.Audio = request.Audio;
        }

        if (request.Metadata != null)
        {
            characterMap.Metadata = request.Metadata;
        }

        characterMap.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(characterMap);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated character map {CharacterMapId}", id);
        return characterMap;
    }

    public async Task<bool> DeleteCharacterMapAsync(string id)
    {
        var characterMap = await _repository.GetByIdAsync(id);
        if (characterMap == null)
        {
            return false;
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted character map {CharacterMapId}", id);
        return true;
    }

    public async Task<string> ExportCharacterMapsAsYamlAsync()
    {
        var characterMaps = (await _repository.GetAllAsync()).ToList();

        var characterMapYaml = new CharacterMapYaml
        {
            Characters = characterMaps.Select(cm => new CharacterMapYamlEntry
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

        return serializer.Serialize(characterMapYaml);
    }

    public async Task<List<CharacterMap>> ImportCharacterMapsFromYamlAsync(Stream yamlStream)
    {
        var deserializer = new DeserializerBuilder()
            .WithCaseInsensitivePropertyMatching()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(yamlStream);
        var yamlContent = await reader.ReadToEndAsync();

        var characterMapYaml = deserializer.Deserialize<CharacterMapYaml>(yamlContent);

        var importedCharacterMaps = new List<CharacterMap>();

        foreach (var yamlEntry in characterMapYaml.Characters)
        {
            var characterMap = new CharacterMap
            {
                Id = yamlEntry.Id,
                Name = yamlEntry.Name,
                Image = yamlEntry.Image,
                Audio = yamlEntry.Audio,
                Metadata = yamlEntry.Metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Check if it exists and update or add
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
