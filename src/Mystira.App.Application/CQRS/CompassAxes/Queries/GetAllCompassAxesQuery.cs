using Mystira.App.Application.Interfaces;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Query to retrieve all compass axes.
/// </summary>
public record GetAllCompassAxesQuery : IQuery<List<CompassAxis>>, ICacheableQuery
{
    public string CacheKey => "MasterData:CompassAxes:All";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
