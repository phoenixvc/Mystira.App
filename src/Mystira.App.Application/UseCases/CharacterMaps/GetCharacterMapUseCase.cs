using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for retrieving a character map by ID
/// </summary>
public class GetCharacterMapUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly ILogger<GetCharacterMapUseCase> _logger;

    public GetCharacterMapUseCase(
        ICharacterMapRepository repository,
        ILogger<GetCharacterMapUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CharacterMap?> ExecuteAsync(string characterMapId)
    {
        if (string.IsNullOrWhiteSpace(characterMapId))
        {
            throw new ArgumentException("Character map ID cannot be null or empty", nameof(characterMapId));
        }

        var characterMap = await _repository.GetByIdAsync(characterMapId);

        if (characterMap == null)
        {
            _logger.LogWarning("Character map not found: {CharacterMapId}", characterMapId);
        }
        else
        {
            _logger.LogDebug("Retrieved character map: {CharacterMapId}", characterMapId);
        }

        return characterMap;
    }
}

