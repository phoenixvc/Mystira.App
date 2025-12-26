using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Wolverine handler for retrieving an archetype by ID.
/// </summary>
public static class GetArchetypeByIdQueryHandler
{
    public static async Task<ArchetypeDefinition?> Handle(
        GetArchetypeByIdQuery query,
        IArchetypeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving archetype with id: {Id}", query.Id);
        var archetype = await repository.GetByIdAsync(query.Id);

        if (archetype == null)
        {
            logger.LogWarning("Archetype with id {Id} not found", query.Id);
        }

        return archetype;
    }
}
