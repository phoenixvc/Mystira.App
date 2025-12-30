using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Wolverine handler for updating an existing age group.
/// </summary>
public static class UpdateAgeGroupCommandHandler
{
    public static async Task<AgeGroupDefinition?> Handle(
        UpdateAgeGroupCommand command,
        IAgeGroupRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Updating age group with id: {Id}", command.Id);

        var existingAgeGroup = await repository.GetByIdAsync(command.Id);
        if (existingAgeGroup == null)
        {
            logger.LogWarning("Age group with id {Id} not found", command.Id);
            return null;
        }

        existingAgeGroup.Name = command.Name;
        existingAgeGroup.Value = command.Value;
        existingAgeGroup.MinimumAge = command.MinimumAge;
        existingAgeGroup.MaximumAge = command.MaximumAge;
        existingAgeGroup.Description = command.Description;
        existingAgeGroup.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existingAgeGroup);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:AgeGroups");

        logger.LogInformation("Successfully updated age group with id: {Id}", command.Id);
        return existingAgeGroup;
    }
}
