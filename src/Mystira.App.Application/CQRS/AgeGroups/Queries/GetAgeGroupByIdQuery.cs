using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Query to retrieve an age group by ID.
/// </summary>
public record GetAgeGroupByIdQuery(string Id) : IQuery<AgeGroupDefinition?>, ICacheableQuery
{
    public string CacheKey => $"MasterData:AgeGroups:{Id}";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
