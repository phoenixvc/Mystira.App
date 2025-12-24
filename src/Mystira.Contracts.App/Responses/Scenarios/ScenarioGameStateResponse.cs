namespace Mystira.Contracts.App.Responses.Scenarios;

public enum ScenarioGameState
{
    NotStarted,
    InProgress,
    Completed
}

public class ScenarioWithGameState
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string SessionLength { get; set; } = string.Empty;
    public List<string> CoreAxes { get; set; } = new();
    public string[] Tags { get; set; } = [];
    public string[] Archetypes { get; set; } = [];
    public ScenarioGameState GameState { get; set; } = ScenarioGameState.NotStarted;
    public DateTime? LastPlayedAt { get; set; }
    public int? PlayCount { get; set; }
    public string? Image { get; set; }
}

public class ScenarioGameStateResponse
{
    public List<ScenarioWithGameState> Scenarios { get; set; } = new();
    public int TotalCount { get; set; }
}

