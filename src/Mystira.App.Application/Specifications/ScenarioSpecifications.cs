using Ardalis.Specification;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Specifications;

/// <summary>
/// Specification to get a scenario by ID.
/// </summary>
public sealed class ScenarioByIdSpec : SingleEntitySpecification<Scenario>
{
    public ScenarioByIdSpec(string id)
    {
        Query.Where(s => s.Id == id);
    }
}

/// <summary>
/// Specification for scenarios by age group.
/// Migrated from ScenariosByAgeGroupSpecification.
/// </summary>
public sealed class ScenariosByAgeGroupSpec : BaseEntitySpecification<Scenario>
{
    public ScenariosByAgeGroupSpec(string ageGroup)
    {
        Query.Where(s => s.AgeGroup == ageGroup)
             .OrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification for scenarios by tag.
/// Migrated from ScenariosByTagSpecification.
/// </summary>
public sealed class ScenariosByTagSpec : BaseEntitySpecification<Scenario>
{
    public ScenariosByTagSpec(string tag)
    {
        Query.Where(s => s.Tags != null && s.Tags.Contains(tag))
             .OrderByDescending(s => s.CreatedAt);
    }
}

/// <summary>
/// Specification for scenarios by difficulty.
/// Migrated from ScenariosByDifficultySpecification.
/// </summary>
public sealed class ScenariosByDifficultySpec : BaseEntitySpecification<Scenario>
{
    public ScenariosByDifficultySpec(DifficultyLevel difficulty)
    {
        Query.Where(s => s.Difficulty == difficulty)
             .OrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification for scenarios with pagination.
/// Migrated from PaginatedScenariosSpecification.
/// </summary>
public sealed class ScenariosPaginatedSpec : BaseEntitySpecification<Scenario>
{
    public ScenariosPaginatedSpec(
        int skip,
        int take,
        string? ageGroup = null,
        DifficultyLevel? difficulty = null,
        string? tag = null)
    {
        var query = Query.AsTracking();

        if (!string.IsNullOrWhiteSpace(ageGroup))
        {
            query = query.Where(s => s.AgeGroup == ageGroup);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(s => s.Difficulty == difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(s => s.Tags != null && s.Tags.Contains(tag));
        }

        query.OrderByDescending(s => s.CreatedAt)
             .Skip(skip)
             .Take(take);
    }
}

/// <summary>
/// Specification for scenarios by archetype.
/// Migrated from ScenariosByArchetypeSpecification.
/// </summary>
public sealed class ScenariosByArchetypeSpec : BaseEntitySpecification<Scenario>
{
    public ScenariosByArchetypeSpec(string archetypeName)
    {
        Query.Where(s => s.Archetypes != null && s.Archetypes.Any(a => a.Value == archetypeName))
             .OrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification for featured scenarios.
/// Migrated from FeaturedScenariosSpecification.
/// </summary>
public sealed class FeaturedScenariosSpec : BaseEntitySpecification<Scenario>
{
    public FeaturedScenariosSpec()
    {
        Query.Where(s => s.Tags != null && s.Tags.Contains("featured"))
             .OrderByDescending(s => s.CreatedAt);
    }
}

/// <summary>
/// Specification to search scenarios by title pattern.
/// </summary>
public sealed class ScenariosByTitlePatternSpec : BaseEntitySpecification<Scenario>
{
    public ScenariosByTitlePatternSpec(string titlePattern)
    {
        Query.Where(s => s.Title.ToLower().Contains(titlePattern.ToLower()))
             .OrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification to get all active scenarios.
/// </summary>
public sealed class PublishedScenariosSpec : BaseEntitySpecification<Scenario>
{
    public PublishedScenariosSpec()
    {
        Query.Where(s => s.IsActive)
             .OrderByDescending(s => s.CreatedAt);
    }
}
