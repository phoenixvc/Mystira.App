using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Wolverine handler for deleting an age group.
/// </summary>
public static class DeleteAgeGroupCommandHandler
{
    public static async Task<bool> Handle(
        DeleteAgeGroupCommand command,
        IAgeGroupRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting age group with id: {Id}", command.Id);

        var ageGroup = await repository.GetByIdAsync(command.Id);
        if (ageGroup == null)
        {
            logger.LogWarning("Age group with id {Id} not found", command.Id);
            return false;
        }

        await repository.DeleteAsync(command.Id);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:AgeGroups");

        logger.LogInformation("Successfully deleted age group with id: {Id}", command.Id);
        return true;
    }
}
