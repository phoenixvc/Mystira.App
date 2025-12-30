using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Wolverine handler for creating a new archetype.
/// </summary>
public static class CreateArchetypeCommandHandler
{
    public static async Task<ArchetypeDefinition> Handle(
        CreateArchetypeCommand command,
        IArchetypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Creating archetype: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var archetype = new ArchetypeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(archetype);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:Archetypes");

        logger.LogInformation("Successfully created archetype with id: {Id}", archetype.Id);
        return archetype;
    }
}
