using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Wolverine handler for updating an existing echo type.
/// </summary>
public static class UpdateEchoTypeCommandHandler
{
    public static async Task<EchoTypeDefinition?> Handle(
        UpdateEchoTypeCommand command,
        IEchoTypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<UpdateEchoTypeCommand> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Updating echo type with id: {Id}", command.Id);

        var existingEchoType = await repository.GetByIdAsync(command.Id);
        if (existingEchoType == null)
        {
            logger.LogWarning("Echo type with id {Id} not found", command.Id);
            return null;
        }

        existingEchoType.Name = command.Name;
        existingEchoType.Description = command.Description;
        existingEchoType.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existingEchoType);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:EchoTypes");

        logger.LogInformation("Successfully updated echo type with id: {Id}", command.Id);
        return existingEchoType;
    }
}
