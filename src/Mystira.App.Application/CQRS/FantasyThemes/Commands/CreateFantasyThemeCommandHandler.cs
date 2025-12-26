using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Wolverine handler for creating a new fantasy theme.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class CreateFantasyThemeCommandHandler
{
    /// <summary>
    /// Handles the CreateFantasyThemeCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<FantasyThemeDefinition> Handle(
        CreateFantasyThemeCommand command,
        IFantasyThemeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Creating fantasy theme: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var fantasyTheme = new FantasyThemeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(fantasyTheme);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        cacheInvalidation.InvalidateCacheByPrefix("MasterData:FantasyThemes");

        logger.LogInformation("Successfully created fantasy theme with id: {Id}", fantasyTheme.Id);
        return fantasyTheme;
    }
}
