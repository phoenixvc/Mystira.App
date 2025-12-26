using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Wolverine handler for deleting a fantasy theme.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class DeleteFantasyThemeCommandHandler
{
    /// <summary>
    /// Handles the DeleteFantasyThemeCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        DeleteFantasyThemeCommand command,
        IFantasyThemeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Deleting fantasy theme with id: {Id}", command.Id);

        var fantasyTheme = await repository.GetByIdAsync(command.Id);
        if (fantasyTheme == null)
        {
            logger.LogWarning("Fantasy theme with id {Id} not found", command.Id);
            return false;
        }

        await repository.DeleteAsync(command.Id);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:FantasyThemes");

        logger.LogInformation("Successfully deleted fantasy theme with id: {Id}", command.Id);
        return true;
    }
}
