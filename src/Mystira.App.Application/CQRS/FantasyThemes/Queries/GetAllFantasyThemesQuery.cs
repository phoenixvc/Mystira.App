using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Query to retrieve all fantasy themes.
/// </summary>
public record GetAllFantasyThemesQuery : IQuery<List<FantasyThemeDefinition>>, ICacheableQuery
{
    public string CacheKey => "MasterData:FantasyThemes:All";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
