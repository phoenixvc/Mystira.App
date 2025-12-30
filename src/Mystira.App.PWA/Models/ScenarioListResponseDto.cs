namespace Mystira.App.PWA.Models;

// DTOs matching the API's ScenarioListResponse and ScenarioSummary
public class ScenarioListResponseDto
{
    public List<ScenarioSummaryDto> Scenarios { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public class ScenarioSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Difficulty { get; set; } = string.Empty; // API sends enum as string; keep as string for PWA model mapping
    public string SessionLength { get; set; } = string.Empty; // API sends enum as string
    public List<string> Archetypes { get; set; } = new();
    public int MinimumAge { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public List<string> CoreAxes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Mystira.Domain.Models.MusicPalette? MusicPalette { get; set; }
}
