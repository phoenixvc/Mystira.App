using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Wolverine handler for updating an existing archetype.
/// </summary>
public static class UpdateArchetypeCommandHandler
{
    public static async Task<ArchetypeDefinition?> Handle(
        UpdateArchetypeCommand command,
        IArchetypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Updating archetype with id: {Id}", command.Id);

        var existingArchetype = await repository.GetByIdAsync(command.Id);
        if (existingArchetype == null)
        {
            logger.LogWarning("Archetype with id {Id} not found", command.Id);
            return null;
        }

        existingArchetype.Name = command.Name;
        existingArchetype.Description = command.Description;
        existingArchetype.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existingArchetype);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:Archetypes");

        logger.LogInformation("Successfully updated archetype with id: {Id}", command.Id);
        return existingArchetype;
    }
}
