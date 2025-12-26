using Ardalis.Specification;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Specifications;

/// <summary>
/// Specification for all active (non-deleted) content bundles.
/// Migrated from ActiveContentBundlesSpecification.
/// </summary>
public sealed class ActiveContentBundlesSpec : BaseEntitySpecification<ContentBundle>
{
    public ActiveContentBundlesSpec()
    {
        Query.Where(b => true) // In future, add soft delete: !b.IsDeleted
             .OrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles by age group.
/// Migrated from ContentBundlesByAgeGroupSpecification.
/// </summary>
public sealed class ContentBundlesByAgeGroupSpec : BaseEntitySpecification<ContentBundle>
{
    public ContentBundlesByAgeGroupSpec(string ageGroup)
    {
        Query.Where(b => b.AgeGroup == ageGroup)
             .OrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for free content bundles.
/// Migrated from FreeContentBundlesSpecification.
/// </summary>
public sealed class FreeContentBundlesSpec : BaseEntitySpecification<ContentBundle>
{
    public FreeContentBundlesSpec()
    {
        Query.Where(b => b.IsFree)
             .OrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles by price range.
/// Migrated from ContentBundlesByPriceRangeSpecification.
/// </summary>
public sealed class ContentBundlesByPriceRangeSpec : BaseEntitySpecification<ContentBundle>
{
    public ContentBundlesByPriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        Query.Where(b => b.Prices.Any(p => p.Value >= minPrice && p.Value <= maxPrice))
             .OrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles containing specific scenarios.
/// Migrated from ContentBundlesByScenarioSpecification.
/// </summary>
public sealed class ContentBundlesByScenarioSpec : BaseEntitySpecification<ContentBundle>
{
    public ContentBundlesByScenarioSpec(string scenarioId)
    {
        Query.Where(b => b.ScenarioIds.Contains(scenarioId))
             .OrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification to get a content bundle by ID.
/// </summary>
public sealed class ContentBundleByIdSpec : SingleEntitySpecification<ContentBundle>
{
    public ContentBundleByIdSpec(string id)
    {
        Query.Where(b => b.Id == id);
    }
}
