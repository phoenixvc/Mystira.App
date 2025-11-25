using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Handler for GetScenarioQuery - retrieves a single scenario by ID
/// This is a read-only operation that doesn't modify state
/// </summary>
public class GetScenarioQueryHandler : IQueryHandler<GetScenarioQuery, Scenario?>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenarioQueryHandler> _logger;

    public GetScenarioQueryHandler(
        IScenarioRepository repository,
        ILogger<GetScenarioQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Scenario?> Handle(GetScenarioQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(request.ScenarioId));
        }

        var scenario = await _repository.GetByIdAsync(request.ScenarioId);

        if (scenario == null)
        {
            _logger.LogWarning("Scenario not found: {ScenarioId}", request.ScenarioId);
        }
        else
        {
            _logger.LogDebug("Retrieved scenario: {ScenarioId}", request.ScenarioId);
        }

        return scenario;
    }
}
