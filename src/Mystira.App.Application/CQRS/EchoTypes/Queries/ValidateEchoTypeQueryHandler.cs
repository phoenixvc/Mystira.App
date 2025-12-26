using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Wolverine handler for validating if an echo type name exists.
/// </summary>
public static class ValidateEchoTypeQueryHandler
{
    public static async Task<bool> Handle(
        ValidateEchoTypeQuery query,
        IEchoTypeRepository repository,
        ILogger<ValidateEchoTypeQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Validating echo type: {Name}", query.Name);
        var isValid = await repository.ExistsByNameAsync(query.Name);
        logger.LogInformation("Echo type '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
