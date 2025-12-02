using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Query to retrieve an archetype by ID.
/// </summary>
public record GetArchetypeByIdQuery(string Id) : IQuery<ArchetypeDefinition?>, ICacheableQuery
{
    public string CacheKey => $"MasterData:Archetypes:{Id}";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
