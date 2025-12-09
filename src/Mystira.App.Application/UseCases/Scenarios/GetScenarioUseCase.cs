using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Scenarios;

/// <summary>
/// Use case for retrieving a single scenario by ID
/// </summary>
public class GetScenarioUseCase
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenarioUseCase> _logger;

    public GetScenarioUseCase(
        IScenarioRepository repository,
        ILogger<GetScenarioUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Scenario?> ExecuteAsync(string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(scenarioId));
        }

        var scenario = await _repository.GetByIdAsync(scenarioId);

        if (scenario == null)
        {
            _logger.LogWarning("Scenario not found: {ScenarioId}", scenarioId);
        }
        else
        {
            _logger.LogDebug("Retrieved scenario: {ScenarioId}", scenarioId);
        }

        return scenario;
    }
}

