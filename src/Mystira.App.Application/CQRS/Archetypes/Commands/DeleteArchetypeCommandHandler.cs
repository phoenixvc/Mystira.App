using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Handler for deleting an archetype.
/// </summary>
public class DeleteArchetypeCommandHandler : ICommandHandler<DeleteArchetypeCommand, bool>
{
    private readonly IArchetypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<DeleteArchetypeCommandHandler> _logger;

    public DeleteArchetypeCommandHandler(
        IArchetypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<DeleteArchetypeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteArchetypeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting archetype with id: {Id}", command.Id);

        var archetype = await _repository.GetByIdAsync(command.Id);
        if (archetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", command.Id);
            return false;
        }

        await _repository.DeleteAsync(command.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:Archetypes");

        _logger.LogInformation("Successfully deleted archetype with id: {Id}", command.Id);
        return true;
    }
}
