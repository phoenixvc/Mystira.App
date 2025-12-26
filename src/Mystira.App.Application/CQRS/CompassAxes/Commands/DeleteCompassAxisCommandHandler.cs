using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Wolverine handler for deleting a compass axis.
/// </summary>
public static class DeleteCompassAxisCommandHandler
{
    public static async Task<bool> Handle(
        DeleteCompassAxisCommand command,
        ICompassAxisRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<DeleteCompassAxisCommand> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting compass axis with id: {Id}", command.Id);

        var axis = await repository.GetByIdAsync(command.Id);
        if (axis == null)
        {
            logger.LogWarning("Compass axis with id {Id} not found", command.Id);
            return false;
        }

        await repository.DeleteAsync(command.Id);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:CompassAxes");

        logger.LogInformation("Successfully deleted compass axis with id: {Id}", command.Id);
        return true;
    }
}
