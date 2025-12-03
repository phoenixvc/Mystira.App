using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Handler for retrieving all compass axes.
/// </summary>
public class GetAllCompassAxesQueryHandler : IQueryHandler<GetAllCompassAxesQuery, List<CompassAxis>>
{
    private readonly ICompassAxisRepository _repository;
    private readonly ILogger<GetAllCompassAxesQueryHandler> _logger;

    public GetAllCompassAxesQueryHandler(
        ICompassAxisRepository repository,
        ILogger<GetAllCompassAxesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<CompassAxis>> Handle(
        GetAllCompassAxesQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all compass axes");
        var axes = await _repository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} compass axes", axes.Count);
        return axes;
    }
}
