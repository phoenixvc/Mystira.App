using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Requests.Scenarios;

public class ScenarioQueryRequest
{
    public DifficultyLevel? Difficulty { get; set; }
    public SessionLength? SessionLength { get; set; }
    public int? MinimumAge { get; set; }
    public string? AgeGroup { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Archetypes { get; set; }
    public List<string>? CoreAxes { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

