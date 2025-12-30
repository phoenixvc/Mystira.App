using Mystira.App.Application.Interfaces;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Query to retrieve all age groups.
/// </summary>
public record GetAllAgeGroupsQuery : IQuery<List<AgeGroupDefinition>>, ICacheableQuery
{
    public string CacheKey => "MasterData:AgeGroups:All";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
