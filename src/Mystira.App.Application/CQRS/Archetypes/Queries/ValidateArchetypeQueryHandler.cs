using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Wolverine handler for validating if an archetype name exists.
/// </summary>
public static class ValidateArchetypeQueryHandler
{
    public static async Task<bool> Handle(
        ValidateArchetypeQuery query,
        IArchetypeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Validating archetype: {Name}", query.Name);
        var isValid = await repository.ExistsByNameAsync(query.Name);
        logger.LogInformation("Archetype '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
