namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents a player's score for a completed scenario.
/// Each profile/scenario pair is scored only once on first completion.
/// </summary>
public class PlayerScenarioScore
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The user profile that completed the scenario
    /// </summary>
    public string ProfileId { get; set; } = string.Empty;
    
    /// <summary>
    /// The scenario that was completed
    /// </summary>
    public string ScenarioId { get; set; } = string.Empty;
    
    /// <summary>
    /// The game session where this score was earned
    /// </summary>
    public string GameSessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Aggregated axis scores (e.g., {"honesty": 15.5, "bravery": -3.2})
    /// </summary>
    public Dictionary<string, float> AxisScores { get; set; } = new();
    
    /// <summary>
    /// When the score was recorded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
