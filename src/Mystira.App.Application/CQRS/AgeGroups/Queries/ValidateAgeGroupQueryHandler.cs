using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Wolverine handler for validating if an age group value exists.
/// </summary>
public static class ValidateAgeGroupQueryHandler
{
    public static async Task<bool> Handle(
        ValidateAgeGroupQuery query,
        IAgeGroupRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Validating age group: {Value}", query.Value);
        var isValid = await repository.ExistsByValueAsync(query.Value);
        logger.LogInformation("Age group '{Value}' is {Status}", query.Value, isValid ? "valid" : "invalid");
        return isValid;
    }
}
