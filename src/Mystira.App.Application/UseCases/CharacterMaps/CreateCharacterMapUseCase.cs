using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.CharacterMaps;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.CharacterMaps;

/// <summary>
/// Use case for creating a new character map
/// </summary>
public class CreateCharacterMapUseCase
{
    private readonly ICharacterMapRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCharacterMapUseCase> _logger;

    public CreateCharacterMapUseCase(
        ICharacterMapRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateCharacterMapUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CharacterMap> ExecuteAsync(CreateCharacterMapRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Check if character map with ID already exists
        var existingCharacterMap = await _repository.GetByIdAsync(request.Id);
        if (existingCharacterMap != null)
        {
            throw new InvalidOperationException($"Character map with ID {request.Id} already exists");
        }

        var characterMap = new CharacterMap
        {
            Id = request.Id,
            Name = request.Name,
            Image = request.Image,
            Audio = request.Audio,
            Metadata = request.Metadata ?? new CharacterMetadata(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(characterMap);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created character map: {CharacterMapId} - {Name}", characterMap.Id, characterMap.Name);
        return characterMap;
    }
}

