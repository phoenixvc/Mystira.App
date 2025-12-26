using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Handler for GetPaginatedScenariosQuery
/// Demonstrates how to use Specification Pattern with pagination
/// </summary>
public class GetPaginatedScenariosQueryHandler : IQueryHandler<GetPaginatedScenariosQuery, IEnumerable<Scenario>>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetPaginatedScenariosQueryHandler> _logger;

    public GetPaginatedScenariosQueryHandler(
        IScenarioRepository repository,
        ILogger<GetPaginatedScenariosQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<Scenario>> Handle(GetPaginatedScenariosQuery request, CancellationToken cancellationToken)
    {
        // Create specification for paginated scenarios
        var spec = new ScenariosPaginatedSpec(
            skip: (request.PageNumber - 1) * request.PageSize,
            take: request.PageSize);

        // Use specification to query repository
        var scenarios = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved page {PageNumber} with {Count} scenarios (page size: {PageSize})",
            request.PageNumber, scenarios.Count(), request.PageSize);

        return scenarios;
    }
}
