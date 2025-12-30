using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Wolverine handler for creating a new compass axis.
/// </summary>
public static class CreateCompassAxisCommandHandler
{
    public static async Task<CompassAxis> Handle(
        CreateCompassAxisCommand command,
        ICompassAxisRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<CreateCompassAxisCommand> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Creating compass axis: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var axis = new CompassAxis
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(axis);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:CompassAxes");

        logger.LogInformation("Successfully created compass axis with id: {Id}", axis.Id);
        return axis;
    }
}
