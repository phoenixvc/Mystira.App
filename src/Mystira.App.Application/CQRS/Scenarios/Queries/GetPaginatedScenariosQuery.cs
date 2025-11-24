using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve paginated scenarios using Specification Pattern
/// Demonstrates CQRS + Specification Pattern with pagination
/// </summary>
public record GetPaginatedScenariosQuery(int PageNumber, int PageSize) : IQuery<IEnumerable<Scenario>>;
