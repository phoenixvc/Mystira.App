using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMaps.Queries;

/// <summary>
/// Handler for retrieving all character maps.
/// Initializes with default characters if the collection is empty.
/// </summary>
public class GetAllCharacterMapsQueryHandler : IQueryHandler<GetAllCharacterMapsQuery, List<CharacterMap>>
{
    private readonly ICharacterMapRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllCharacterMapsQueryHandler> _logger;

    public GetAllCharacterMapsQueryHandler(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<GetAllCharacterMapsQueryHandler> _logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<List<CharacterMap>> Handle(
        GetAllCharacterMapsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all character maps");

        var characterMaps = (await _repository.GetAllAsync()).ToList();

        // Initialize with default data if empty
        if (!characterMaps.Any())
        {
            await InitializeDefaultCharacterMapsAsync(cancellationToken);
            characterMaps = (await _repository.GetAllAsync()).ToList();
        }

        _logger.LogInformation("Found {Count} character maps", characterMaps.Count);
        return characterMaps;
    }

    private async Task InitializeDefaultCharacterMapsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing default character maps");

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

        await _repository.AddAsync(elarion);
        await _repository.AddAsync(grubb);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Initialized 2 default character maps");
    }
}
