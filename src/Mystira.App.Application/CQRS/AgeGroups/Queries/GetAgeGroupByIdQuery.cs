using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Query to retrieve an age group by ID.
/// </summary>
public record GetAgeGroupByIdQuery(string Id) : IQuery<AgeGroupDefinition?>;
