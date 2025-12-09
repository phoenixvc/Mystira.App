using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Query to retrieve a fantasy theme by ID.
/// </summary>
public record GetFantasyThemeByIdQuery(string Id) : IQuery<FantasyThemeDefinition?>, ICacheableQuery
{
    public string CacheKey => $"MasterData:FantasyThemes:{Id}";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
