using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Wolverine handler for updating an existing fantasy theme.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class UpdateFantasyThemeCommandHandler
{
    /// <summary>
    /// Handles the UpdateFantasyThemeCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<FantasyThemeDefinition?> Handle(
        UpdateFantasyThemeCommand command,
        IFantasyThemeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Updating fantasy theme with id: {Id}", command.Id);

        var existingFantasyTheme = await repository.GetByIdAsync(command.Id);
        if (existingFantasyTheme == null)
        {
            logger.LogWarning("Fantasy theme with id {Id} not found", command.Id);
            return null;
        }

        existingFantasyTheme.Name = command.Name;
        existingFantasyTheme.Description = command.Description;
        existingFantasyTheme.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existingFantasyTheme);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:FantasyThemes");

        logger.LogInformation("Successfully updated fantasy theme with id: {Id}", command.Id);
        return existingFantasyTheme;
    }
}
