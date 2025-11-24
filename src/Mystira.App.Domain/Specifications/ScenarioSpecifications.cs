using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification for scenarios by age group
/// </summary>
public class ScenariosByAgeGroupSpecification : BaseSpecification<Scenario>
{
    public ScenariosByAgeGroupSpecification(string ageGroup)
        : base(s => s.AgeGroup == ageGroup)
    {
        ApplyOrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification for scenarios by tag
/// </summary>
public class ScenariosByTagSpecification : BaseSpecification<Scenario>
{
    public ScenariosByTagSpecification(string tag)
        : base(s => s.Tags != null && s.Tags.Contains(tag))
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}

/// <summary>
/// Specification for scenarios by difficulty
/// </summary>
public class ScenariosByDifficultySpecification : BaseSpecification<Scenario>
{
    public ScenariosByDifficultySpecification(DifficultyLevel difficulty)
        : base(s => s.Difficulty == difficulty)
    {
        ApplyOrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification for scenarios with pagination
/// </summary>
public class PaginatedScenariosSpecification : BaseSpecification<Scenario>
{
    public PaginatedScenariosSpecification(int pageNumber, int pageSize)
        : base(s => true)
    {
        ApplyOrderByDescending(s => s.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Specification for scenarios by archetype
/// </summary>
public class ScenariosByArchetypeSpecification : BaseSpecification<Scenario>
{
    public ScenariosByArchetypeSpecification(string archetypeName)
        : base(s => s.Archetypes != null && s.Archetypes.Any(a => a.Value == archetypeName))
    {
        ApplyOrderBy(s => s.Title);
    }
}

/// <summary>
/// Specification for featured scenarios (high quality, curated content)
/// Example of a composite specification with multiple criteria
/// </summary>
public class FeaturedScenariosSpecification : BaseSpecification<Scenario>
{
    public FeaturedScenariosSpecification()
        : base(s =>
            s.Tags != null &&
            s.Tags.Contains("featured"))
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
