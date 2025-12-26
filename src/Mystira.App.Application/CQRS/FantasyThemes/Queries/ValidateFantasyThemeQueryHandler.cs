using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Wolverine handler for validating if a fantasy theme name exists.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class ValidateFantasyThemeQueryHandler
{
    /// <summary>
    /// Handles the ValidateFantasyThemeQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        ValidateFantasyThemeQuery query,
        IFantasyThemeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Validating fantasy theme: {Name}", query.Name);
        var isValid = await repository.ExistsByNameAsync(query.Name);
        logger.LogInformation("Fantasy theme '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
