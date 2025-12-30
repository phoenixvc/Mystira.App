using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMaps.Queries;

/// <summary>
/// Wolverine handler for retrieving all character maps.
/// Initializes with default characters if the collection is empty.
/// </summary>
public static class GetAllCharacterMapsQueryHandler
{
    public static async Task<List<CharacterMap>> Handle(
        GetAllCharacterMapsQuery query,
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<GetAllCharacterMapsQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving all character maps");

        var characterMaps = (await repository.GetAllAsync()).ToList();

        // Initialize with default data if empty
        if (!characterMaps.Any())
        {
            await InitializeDefaultCharacterMapsAsync(repository, unitOfWork, logger, ct);
            characterMaps = (await repository.GetAllAsync()).ToList();
        }

        logger.LogInformation("Found {Count} character maps", characterMaps.Count);
        return characterMaps;
    }

    private static async Task InitializeDefaultCharacterMapsAsync(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<GetAllCharacterMapsQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Initializing default character maps");

        var elarion = new CharacterMap
        {
            Id = "elarion",
            Name = "Elarion the Wise",
            Image = "media/images/elarion.jpg",
            Audio = "media/audio/elarion_voice.mp3",
            Metadata = new CharacterMetadata
            {
                Roles = ["mentor", "peacemaker"],
                Archetypes = ["guardian", "quiet strength"],
                Species = "elf",
                Age = 312,
                Traits = ["wise", "calm", "mysterious"],
                Backstory = "A sage from the Verdant Isles who guides lost heroes."
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var grubb = new CharacterMap
        {
            Id = "grubb",
            Name = "Grubb the Goblin",
            Image = "media/images/grubb.png",
            Audio = "media/audio/grubb_laugh.mp3",
            Metadata = new CharacterMetadata
            {
                Roles = ["trickster", "sly"],
                Archetypes = ["sneaky foe"],
                Species = "goblin",
                Age = 14,
                Traits = ["sneaky", "funny", "chaotic"],
                Backstory = "An outcast goblin who joins adventurers for laughs and loot."
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(elarion);
        await repository.AddAsync(grubb);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Initialized 2 default character maps");
    }
}
