using Mystira.Shared.Caching;

namespace Mystira.App.Application.Interfaces;

/// <summary>
/// Marker interface for queries that should be cached.
/// This is an alias for <see cref="ICachedQuery"/> from Mystira.Shared.Caching.
/// New queries should implement ICachedQuery directly.
/// </summary>
[Obsolete("Use Mystira.Shared.Caching.ICachedQuery directly")]
public interface ICacheableQuery : ICachedQuery
{
}
