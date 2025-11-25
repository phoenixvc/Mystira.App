using System.Linq.Expressions;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification pattern for encapsulating query logic
/// Specifications are composable and reusable query definitions
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Criteria expression for filtering entities (WHERE clause)
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Include expressions for eager loading related entities
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Include strings for eager loading related entities using ThenInclude
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression for ascending sort
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression for descending sort
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Group by expression
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Number of records to skip (for pagination)
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Number of records to take (for pagination)
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Enable pagination
    /// </summary>
    bool IsPagingEnabled { get; }
}
