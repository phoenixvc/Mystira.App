using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMaps.Queries;

/// <summary>
/// Wolverine handler for retrieving a specific character map by ID.
/// </summary>
public static class GetCharacterMapQueryHandler
{
    public static async Task<CharacterMap?> Handle(
        GetCharacterMapQuery query,
        ICharacterMapRepository repository,
        ILogger<GetCharacterMapQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving character map {CharacterMapId}", query.Id);

        var characterMap = await repository.GetByIdAsync(query.Id);

        if (characterMap == null)
        {
            logger.LogWarning("Character map not found: {CharacterMapId}", query.Id);
        }

        return characterMap;
    }
}
