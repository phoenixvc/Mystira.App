using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Wolverine handler for deleting an archetype.
/// </summary>
public static class DeleteArchetypeCommandHandler
{
    public static async Task<bool> Handle(
        DeleteArchetypeCommand command,
        IArchetypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting archetype with id: {Id}", command.Id);

        var archetype = await repository.GetByIdAsync(command.Id);
        if (archetype == null)
        {
            logger.LogWarning("Archetype with id {Id} not found", command.Id);
            return false;
        }

        await repository.DeleteAsync(command.Id);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:Archetypes");

        logger.LogInformation("Successfully deleted archetype with id: {Id}", command.Id);
        return true;
    }
}
