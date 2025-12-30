using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Wolverine handler for retrieving all archetypes.
/// </summary>
public static class GetAllArchetypesQueryHandler
{
    public static async Task<List<ArchetypeDefinition>> Handle(
        GetAllArchetypesQuery query,
        IArchetypeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving all archetypes");
        var archetypes = await repository.GetAllAsync();
        logger.LogInformation("Retrieved {Count} archetypes", archetypes.Count);
        return archetypes;
    }
}
