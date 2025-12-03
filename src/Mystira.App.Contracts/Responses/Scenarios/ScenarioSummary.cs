using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Responses.Scenarios;

public class ScenarioSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DifficultyLevel Difficulty { get; set; }
    public SessionLength SessionLength { get; set; }
    public List<string> Archetypes { get; set; } = new();
    public int MinimumAge { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public List<string> CoreAxes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? Image { get; set; }
    }

