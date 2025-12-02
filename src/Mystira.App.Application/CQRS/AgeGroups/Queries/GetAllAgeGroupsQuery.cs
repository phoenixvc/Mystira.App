using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Query to retrieve all age groups.
/// </summary>
public record GetAllAgeGroupsQuery : IQuery<List<AgeGroupDefinition>>;
