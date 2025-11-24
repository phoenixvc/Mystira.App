using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification for all active (non-deleted) content bundles
/// Ordered by title ascending
/// </summary>
public class ActiveContentBundlesSpecification : BaseSpecification<ContentBundle>
{
    public ActiveContentBundlesSpecification()
        : base(b => true) // In future, add soft delete: !b.IsDeleted
    {
        ApplyOrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles by age group
/// Filters by age group and excludes deleted bundles
/// Ordered by title ascending
/// </summary>
public class ContentBundlesByAgeGroupSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByAgeGroupSpecification(string ageGroup)
        : base(b => b.AgeGroup == ageGroup) // In future, add: && !b.IsDeleted
    {
        ApplyOrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for free content bundles
/// Filters bundles marked as free
/// Ordered by title ascending
/// </summary>
public class FreeContentBundlesSpecification : BaseSpecification<ContentBundle>
{
    public FreeContentBundlesSpecification()
        : base(b => b.IsFree) // In future, add: && !b.IsDeleted
    {
        ApplyOrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles by price range
/// Filters bundles with at least one price in the specified range
/// Ordered by lowest price ascending
/// </summary>
public class ContentBundlesByPriceRangeSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByPriceRangeSpecification(decimal minPrice, decimal maxPrice)
        : base(b => b.Prices.Any(p => p.Value >= minPrice && p.Value <= maxPrice)) // In future, add: && !b.IsDeleted
    {
        // Note: Ordering by nested collection property would require custom logic
        ApplyOrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles containing specific scenarios
/// Filters bundles that include the specified scenario ID
/// Ordered by title ascending
/// </summary>
public class ContentBundlesByScenarioSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByScenarioSpecification(string scenarioId)
        : base(b => b.ScenarioIds.Contains(scenarioId)) // In future, add: && !b.IsDeleted
    {
        ApplyOrderBy(b => b.Title);
    }
}
