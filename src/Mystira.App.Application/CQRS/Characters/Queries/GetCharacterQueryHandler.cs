using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Characters.Queries;

/// <summary>
/// Handler for retrieving a specific character by ID.
/// Searches through the CharacterMapFile to find the requested character and maps to API model.
/// </summary>
public class GetCharacterQueryHandler : IQueryHandler<GetCharacterQuery, Character?>
{
    private readonly ICharacterMapFileRepository _repository;
    private readonly ILogger<GetCharacterQueryHandler> _logger;

    public GetCharacterQueryHandler(
        ICharacterMapFileRepository repository,
        ILogger<GetCharacterQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Character?> Handle(
        GetCharacterQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting character with ID: {CharacterId}", request.CharacterId);

        var characterMapFile = await _repository.GetAsync();

        if (characterMapFile == null)
        {
            _logger.LogWarning("Character map file not found");
            return null;
        }

        var domainCharacter = characterMapFile.Characters.FirstOrDefault(c => c.Id == request.CharacterId);

        if (domainCharacter == null)
        {
            _logger.LogWarning("Character not found: {CharacterId}", request.CharacterId);
            return null;
        }

        _logger.LogInformation("Found character: {CharacterName}", domainCharacter.Name);

        // Map from Domain CharacterMapFileCharacter to API Character model
        return new Character
        {
            Id = domainCharacter.Id,
            Name = domainCharacter.Name,
            Image = domainCharacter.Image,
            Metadata = new CharacterMetadata
            {
                Roles = domainCharacter.Metadata.Roles,
                Archetypes = domainCharacter.Metadata.Archetypes,
                Species = domainCharacter.Metadata.Species,
                Age = domainCharacter.Metadata.Age,
                Traits = domainCharacter.Metadata.Traits,
                Backstory = domainCharacter.Metadata.Backstory
            }
        };
    }
}
