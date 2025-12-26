using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Wolverine handler for deleting an echo type.
/// </summary>
public static class DeleteEchoTypeCommandHandler
{
    public static async Task<bool> Handle(
        DeleteEchoTypeCommand command,
        IEchoTypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<DeleteEchoTypeCommand> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting echo type with id: {Id}", command.Id);

        var echoType = await repository.GetByIdAsync(command.Id);
        if (echoType == null)
        {
            logger.LogWarning("Echo type with id {Id} not found", command.Id);
            return false;
        }

        await repository.DeleteAsync(command.Id);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:EchoTypes");

        logger.LogInformation("Successfully deleted echo type with id: {Id}", command.Id);
        return true;
    }
}
