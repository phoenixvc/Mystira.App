using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Handler for retrieving a compass axis by ID.
/// </summary>
public class GetCompassAxisByIdQueryHandler : IQueryHandler<GetCompassAxisByIdQuery, CompassAxis?>
{
    private readonly ICompassAxisRepository _repository;
    private readonly ILogger<GetCompassAxisByIdQueryHandler> _logger;

    public GetCompassAxisByIdQueryHandler(
        ICompassAxisRepository repository,
        ILogger<GetCompassAxisByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CompassAxis?> Handle(
        GetCompassAxisByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving compass axis with id: {Id}", query.Id);
        var axis = await _repository.GetByIdAsync(query.Id);
        
        if (axis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", query.Id);
        }
        
        return axis;
    }
}
