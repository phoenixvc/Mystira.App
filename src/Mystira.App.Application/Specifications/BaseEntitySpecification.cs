using Ardalis.Specification;

namespace Mystira.App.Application.Specifications;

/// <summary>
/// Base specification class using Ardalis.Specification.
/// Provides a foundation for all entity-specific specifications.
///
/// Migration from existing BaseSpecification:
/// - Replace `BaseSpecification` inheritance with `Specification` (for lists) or `SingleResultSpecification` (for single results)
/// - Move criteria from constructor to Query.Where()
/// - Replace ApplyOrderBy with Query.OrderBy()
/// - Replace ApplyPaging with Query.Skip().Take()
///
/// Example migration:
/// OLD:
///   public class ProfilesByAccountSpecification : BaseSpecification{UserProfile}
///   {
///       public ProfilesByAccountSpecification(string accountId)
///           : base(p => p.AccountId == accountId)
///       {
///           ApplyOrderBy(p => p.Name);
///       }
///   }
///
/// NEW:
///   public class ProfilesByAccountSpec : Specification{UserProfile}
///   {
///       public ProfilesByAccountSpec(string accountId)
///       {
///           Query.Where(p => p.AccountId == accountId)
///                .OrderBy(p => p.Name);
///       }
///   }
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class BaseEntitySpecification<T> : Specification<T> where T : class
{
    /// <summary>
    /// Enable caching for this specification with the given key.
    /// Useful for frequently accessed, rarely changing data.
    /// </summary>
    /// <param name="cacheKey">The cache key for this specification result</param>
    /// <param name="slidingExpiration">Optional sliding expiration (stored in cache key for downstream use)</param>
    protected void EnableCaching(string cacheKey, TimeSpan? slidingExpiration = null)
    {
        // Ardalis.Specification's EnableCache only accepts a cache key.
        // If sliding expiration is needed, encode it in the cache key for downstream cache providers.
        var effectiveKey = slidingExpiration.HasValue
            ? $"{cacheKey}|exp:{(int)slidingExpiration.Value.TotalSeconds}"
            : cacheKey;
        Query.EnableCache(effectiveKey);
    }
}

/// <summary>
/// Base specification for queries that return a single result.
/// Use this for GetById, GetByEmail, etc.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class SingleEntitySpecification<T> : SingleResultSpecification<T> where T : class
{
}

/// <summary>
/// Base specification with projection support.
/// Use when you need to project to a different type (e.g., DTOs).
/// </summary>
/// <typeparam name="T">The source entity type</typeparam>
/// <typeparam name="TResult">The projected result type</typeparam>
public abstract class ProjectedSpecification<T, TResult> : Specification<T, TResult> where T : class
{
}
