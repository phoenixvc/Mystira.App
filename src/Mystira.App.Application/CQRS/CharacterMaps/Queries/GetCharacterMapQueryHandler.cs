using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMaps.Queries;

/// <summary>
/// Handler for retrieving a specific character map by ID.
/// </summary>
public class GetCharacterMapQueryHandler : IQueryHandler<GetCharacterMapQuery, CharacterMap?>
{
    private readonly ICharacterMapRepository _repository;
    private readonly ILogger<GetCharacterMapQueryHandler> _logger;

    public GetCharacterMapQueryHandler(
        ICharacterMapRepository repository,
        ILogger<GetCharacterMapQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CharacterMap?> Handle(
        GetCharacterMapQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving character map {CharacterMapId}", query.Id);

        var characterMap = await _repository.GetByIdAsync(query.Id);

        if (characterMap == null)
        {
            _logger.LogWarning("Character map not found: {CharacterMapId}", query.Id);
        }

        return characterMap;
    }
}
