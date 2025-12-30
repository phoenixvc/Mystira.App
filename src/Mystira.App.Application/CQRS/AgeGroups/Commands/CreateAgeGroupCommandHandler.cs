using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Wolverine handler for creating a new age group.
/// </summary>
public static class CreateAgeGroupCommandHandler
{
    public static async Task<AgeGroupDefinition> Handle(
        CreateAgeGroupCommand command,
        IAgeGroupRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Creating age group: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        if (string.IsNullOrWhiteSpace(command.Value))
        {
            throw new ArgumentException("Value is required");
        }

        var ageGroup = new AgeGroupDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Value = command.Value,
            MinimumAge = command.MinimumAge,
            MaximumAge = command.MaximumAge,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(ageGroup);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:AgeGroups");

        logger.LogInformation("Successfully created age group with id: {Id}", ageGroup.Id);
        return ageGroup;
    }
}
