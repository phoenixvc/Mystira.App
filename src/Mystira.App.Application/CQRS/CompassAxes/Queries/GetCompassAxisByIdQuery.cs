using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Query to retrieve a compass axis by ID.
/// </summary>
public record GetCompassAxisByIdQuery(string Id) : IQuery<CompassAxis?>, ICacheableQuery
{
    public string CacheKey => $"MasterData:CompassAxes:{Id}";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
