using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve a single scenario by ID (read operation)
/// </summary>
public record GetScenarioQuery(string ScenarioId) : IQuery<Scenario?>;
