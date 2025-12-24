using Mystira.Contracts.App.Models.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.Contracts.App.Responses.GameSessions;

public class GameSessionResponse
{
    public string Id { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public List<string> PlayerNames { get; set; } = new();
    public List<CharacterAssignmentDto> CharacterAssignments { get; set; } = new();
    public List<PlayerCompassProgressDto> PlayerCompassProgressTotals { get; set; } = new();
    public SessionStatus Status { get; set; }
    public string CurrentSceneId { get; set; } = string.Empty;
    public int ChoiceCount { get; set; }
    public int EchoCount { get; set; }
    public int AchievementCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public bool IsPaused { get; set; }
    public int SceneCount { get; set; }
    public string TargetAgeGroup { get; set; } = string.Empty;
}
