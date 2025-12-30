using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Wolverine handler for retrieving a compass axis by ID.
/// </summary>
public static class GetCompassAxisByIdQueryHandler
{
    public static async Task<CompassAxis?> Handle(
        GetCompassAxisByIdQuery query,
        ICompassAxisRepository repository,
        ILogger<GetCompassAxisByIdQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving compass axis with id: {Id}", query.Id);
        var axis = await repository.GetByIdAsync(query.Id);

        if (axis == null)
        {
            logger.LogWarning("Compass axis with id {Id} not found", query.Id);
        }

        return axis;
    }
}
