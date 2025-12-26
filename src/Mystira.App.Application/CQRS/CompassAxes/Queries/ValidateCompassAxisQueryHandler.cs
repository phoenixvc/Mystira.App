using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Wolverine handler for validating if a compass axis name exists.
/// </summary>
public static class ValidateCompassAxisQueryHandler
{
    public static async Task<bool> Handle(
        ValidateCompassAxisQuery query,
        ICompassAxisRepository repository,
        ILogger<ValidateCompassAxisQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Validating compass axis: {Name}", query.Name);
        var isValid = await repository.ExistsByNameAsync(query.Name);
        logger.LogInformation("Compass axis '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
