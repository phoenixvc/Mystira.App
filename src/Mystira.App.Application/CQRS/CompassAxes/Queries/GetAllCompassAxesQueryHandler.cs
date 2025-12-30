using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Wolverine handler for retrieving all compass axes.
/// </summary>
public static class GetAllCompassAxesQueryHandler
{
    public static async Task<List<CompassAxis>> Handle(
        GetAllCompassAxesQuery query,
        ICompassAxisRepository repository,
        ILogger<GetAllCompassAxesQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving all compass axes");
        var axes = await repository.GetAllAsync();
        logger.LogInformation("Retrieved {Count} compass axes", axes.Count);
        return axes;
    }
}
