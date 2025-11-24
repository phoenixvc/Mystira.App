using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Infrastructure.Data.Specifications;

/// <summary>
/// Evaluates specifications and applies them to EF Core queries
/// This bridges the gap between domain specifications and EF Core queries
/// </summary>
public static class SpecificationEvaluator<T> where T : class
{
    /// <summary>
    /// Apply a specification to an IQueryable
    /// </summary>
    /// <param name="inputQuery">The input query</param>
    /// <param name="specification">The specification to apply</param>
    /// <returns>The query with the specification applied</returns>
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        // Apply criteria (WHERE clause)
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes (eager loading)
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply include strings (for ThenInclude scenarios)
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
