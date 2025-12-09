using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMaps.Queries;

/// <summary>
/// Query to retrieve all character maps.
/// Initializes with default character data if empty.
/// </summary>
public record GetAllCharacterMapsQuery : IQuery<List<CharacterMap>>, ICacheableQuery
{
    public string CacheKey => "AllCharacterMaps";
    public int CacheDurationSeconds => 300; // 5 minutes
}
