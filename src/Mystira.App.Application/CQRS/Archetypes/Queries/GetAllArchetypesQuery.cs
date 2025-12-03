using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Query to retrieve all archetypes.
/// </summary>
public record GetAllArchetypesQuery : IQuery<List<ArchetypeDefinition>>, ICacheableQuery
{
    public string CacheKey => "MasterData:Archetypes:All";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
