using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.CharacterMaps;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for updating a character map
/// </summary>
public class UpdateCharacterMapUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCharacterMapUseCase> _logger;

    public UpdateCharacterMapUseCase(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCharacterMapUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CharacterMap> ExecuteAsync(string characterMapId, UpdateCharacterMapRequest request)
    {
        if (string.IsNullOrWhiteSpace(characterMapId))
        {
            throw new ArgumentException("Character map ID cannot be null or empty", nameof(characterMapId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var characterMap = await _repository.GetByIdAsync(characterMapId);
        if (characterMap == null)
        {
            throw new ArgumentException($"Character map not found: {characterMapId}", nameof(characterMapId));
        }

        // Update properties if provided
        if (request.Name != null)
        {
            characterMap.Name = request.Name;
        }

        if (request.Image != null)
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

        _logger.LogInformation("Updated character map: {CharacterMapId}", characterMapId);
        return characterMap;
    }
}

