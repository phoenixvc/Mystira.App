using Mystira.Domain.Models;

namespace Mystira.App.Application.Services;

/// <summary>
/// Service for evaluating and awarding badges based on axis scores
/// </summary>
public interface IBadgeAwardingService
{
    /// <summary>
    /// Evaluate a profile's axis scores against their age group's badge tiers
    /// and award any newly earned badges.
    /// Never downgrades or removes previously earned badges.
    /// </summary>
    /// <param name="profile">The user profile</param>
    /// <param name="axisScores">The current axis scores (per axis)</param>
    /// <returns>List of newly awarded UserBadge entities</returns>
    Task<List<UserBadge>> AwardBadgesAsync(UserProfile profile, Dictionary<string, float> axisScores);
}
