using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Handler for updating an existing archetype.
/// </summary>
public class UpdateArchetypeCommandHandler : ICommandHandler<UpdateArchetypeCommand, ArchetypeDefinition?>
{
    private readonly IArchetypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<UpdateArchetypeCommandHandler> _logger;

    public UpdateArchetypeCommandHandler(
        IArchetypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<UpdateArchetypeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<ArchetypeDefinition?> Handle(
        UpdateArchetypeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating archetype with id: {Id}", command.Id);

        var existingArchetype = await _repository.GetByIdAsync(command.Id);
        if (existingArchetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", command.Id);
            return null;
        }

        existingArchetype.Name = command.Name;
        existingArchetype.Description = command.Description;
        existingArchetype.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingArchetype);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:Archetypes");

        _logger.LogInformation("Successfully updated archetype with id: {Id}", command.Id);
        return existingArchetype;
    }
}
