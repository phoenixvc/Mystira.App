using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Characters.Queries;

/// <summary>
/// Wolverine handler for retrieving a specific character by ID.
/// Searches through the CharacterMapFile to find the requested character and maps to API model.
/// </summary>
public static class GetCharacterQueryHandler
{
    public static async Task<Character?> Handle(
        GetCharacterQuery request,
        ICharacterMapFileRepository repository,
        ILogger<GetCharacterQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting character with ID: {CharacterId}", request.CharacterId);

        var characterMapFile = await repository.GetAsync();

        if (characterMapFile == null)
        {
            logger.LogWarning("Character map file not found");
            return null;
        }

        var domainCharacter = characterMapFile.Characters.FirstOrDefault(c => c.Id == request.CharacterId);

        if (domainCharacter == null)
        {
            logger.LogWarning("Character not found: {CharacterId}", request.CharacterId);
            return null;
        }

        logger.LogInformation("Found character: {CharacterName}", domainCharacter.Name);

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
