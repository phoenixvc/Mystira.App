using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Services;

/// <summary>
/// Service for computing and persisting scenario scores upon session completion
/// </summary>
public interface IAxisScoringService
{
    /// <summary>
    /// Score a completed game session for a profile.
    /// Aggregates per-choice axis deltas and creates a PlayerScenarioScore record.
    /// Skips if the profile/scenario pair has already been scored.
    /// </summary>
    /// <param name="session">The completed game session</param>
    /// <param name="profile">The player profile</param>
    /// <returns>The created PlayerScenarioScore or null if already scored</returns>
    Task<PlayerScenarioScore?> ScoreSessionAsync(GameSession session, UserProfile profile);
}
