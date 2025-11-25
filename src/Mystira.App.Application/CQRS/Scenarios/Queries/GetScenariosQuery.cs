using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve all scenarios (read operation)
/// Can be extended with filters, pagination, etc.
/// </summary>
public record GetScenariosQuery : IQuery<IEnumerable<Scenario>>;
