using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Wolverine handler for creating a new echo type.
/// </summary>
public static class CreateEchoTypeCommandHandler
{
    public static async Task<EchoTypeDefinition> Handle(
        CreateEchoTypeCommand command,
        IEchoTypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<CreateEchoTypeCommand> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Creating echo type: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var echoType = new EchoTypeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(echoType);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:EchoTypes");

        logger.LogInformation("Successfully created echo type with id: {Id}", echoType.Id);
        return echoType;
    }
}
