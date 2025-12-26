using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Wolverine handler for retrieving all fantasy themes.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetAllFantasyThemesQueryHandler
{
    /// <summary>
    /// Handles the GetAllFantasyThemesQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<FantasyThemeDefinition>> Handle(
        GetAllFantasyThemesQuery query,
        IFantasyThemeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving all fantasy themes");
        var fantasyThemes = await repository.GetAllAsync();
        logger.LogInformation("Retrieved {Count} fantasy themes", fantasyThemes.Count);
        return fantasyThemes;
    }
}
