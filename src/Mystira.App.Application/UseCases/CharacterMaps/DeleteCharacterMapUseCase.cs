using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for deleting a character map
/// </summary>
public class DeleteCharacterMapUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCharacterMapUseCase> _logger;

    public DeleteCharacterMapUseCase(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCharacterMapUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string characterMapId)
    {
        if (string.IsNullOrWhiteSpace(characterMapId))
        {
            throw new ArgumentException("Character map ID cannot be null or empty", nameof(characterMapId));
        }

        var characterMap = await _repository.GetByIdAsync(characterMapId);
        if (characterMap == null)
        {
            _logger.LogWarning("Character map not found for deletion: {CharacterMapId}", characterMapId);
            return false;
        }

        await _repository.DeleteAsync(characterMapId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted character map: {CharacterMapId}", characterMapId);
        return true;
    }
}

