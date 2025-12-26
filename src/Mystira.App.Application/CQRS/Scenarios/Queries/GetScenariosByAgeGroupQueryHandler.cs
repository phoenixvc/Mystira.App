using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Handler for GetScenariosByAgeGroupQuery
/// Demonstrates how to use Specification Pattern with CQRS queries
/// </summary>
public class GetScenariosByAgeGroupQueryHandler : IQueryHandler<GetScenariosByAgeGroupQuery, IEnumerable<Scenario>>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenariosByAgeGroupQueryHandler> _logger;

    public GetScenariosByAgeGroupQueryHandler(
        IScenarioRepository repository,
        ILogger<GetScenariosByAgeGroupQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<Scenario>> Handle(GetScenariosByAgeGroupQuery request, CancellationToken cancellationToken)
    {
        // Create specification for scenarios by age group
        var spec = new ScenariosByAgeGroupSpec(request.AgeGroup);

        // Use specification to query repository
        var scenarios = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} scenarios for age group: {AgeGroup}",
            scenarios.Count(), request.AgeGroup);

        return scenarios;
    }
}
