using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Handler for GetScenariosQuery - retrieves all scenarios
/// This is a read-only operation that doesn't modify state
/// </summary>
public class GetScenariosQueryHandler : IQueryHandler<GetScenariosQuery, IEnumerable<Scenario>>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenariosQueryHandler> _logger;

    public GetScenariosQueryHandler(
        IScenarioRepository repository,
        ILogger<GetScenariosQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<Scenario>> Handle(GetScenariosQuery request, CancellationToken cancellationToken)
    {
        var scenarios = await _repository.GetAllAsync();

        _logger.LogDebug("Retrieved {Count} scenarios", scenarios.Count());

        return scenarios;
    }
}
