using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for retrieving all character maps
/// </summary>
public class GetCharacterMapsUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly ILogger<GetCharacterMapsUseCase> _logger;

    public GetCharacterMapsUseCase(
        ICharacterMapRepository repository,
        ILogger<GetCharacterMapsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<CharacterMap>> ExecuteAsync()
    {
        var characterMaps = await _repository.GetAllAsync();
        var characterMapList = characterMaps.ToList();

        _logger.LogInformation("Retrieved {Count} character maps", characterMapList.Count);
        return characterMapList;
    }
}

