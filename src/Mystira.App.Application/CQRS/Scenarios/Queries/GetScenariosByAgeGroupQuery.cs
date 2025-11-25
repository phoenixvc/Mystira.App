using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve scenarios by age group using Specification Pattern
/// Demonstrates CQRS + Specification Pattern integration
/// </summary>
public record GetScenariosByAgeGroupQuery(string AgeGroup) : IQuery<IEnumerable<Scenario>>;
