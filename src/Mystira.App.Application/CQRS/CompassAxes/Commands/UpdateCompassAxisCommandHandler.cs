using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Wolverine handler for updating an existing compass axis.
/// </summary>
public static class UpdateCompassAxisCommandHandler
{
    public static async Task<CompassAxis?> Handle(
        UpdateCompassAxisCommand command,
        ICompassAxisRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<UpdateCompassAxisCommand> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Updating compass axis with id: {Id}", command.Id);

        var existingAxis = await repository.GetByIdAsync(command.Id);
        if (existingAxis == null)
        {
            logger.LogWarning("Compass axis with id {Id} not found", command.Id);
            return null;
        }

        existingAxis.Name = command.Name;
        existingAxis.Description = command.Description;
        existingAxis.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existingAxis);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:CompassAxes");

        logger.LogInformation("Successfully updated compass axis with id: {Id}", command.Id);
        return existingAxis;
    }
}
