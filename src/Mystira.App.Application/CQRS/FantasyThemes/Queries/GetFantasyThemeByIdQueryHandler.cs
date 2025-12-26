using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Wolverine handler for retrieving a fantasy theme by ID.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetFantasyThemeByIdQueryHandler
{
    /// <summary>
    /// Handles the GetFantasyThemeByIdQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<FantasyThemeDefinition?> Handle(
        GetFantasyThemeByIdQuery query,
        IFantasyThemeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving fantasy theme with id: {Id}", query.Id);
        var fantasyTheme = await repository.GetByIdAsync(query.Id);

        if (fantasyTheme == null)
        {
            logger.LogWarning("Fantasy theme with id {Id} not found", query.Id);
        }

        return fantasyTheme;
    }
}
