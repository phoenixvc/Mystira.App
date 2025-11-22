using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public interface IUserBadgeApiService
{
    /// <summary>
    /// Award a badge to a user profile
    /// </summary>
    /// <param name="request">Badge award request</param>
    /// <returns>The awarded badge</returns>
    Task<UserBadge> AwardBadgeAsync(AwardBadgeRequest request);

    /// <summary>
    /// Get all badges for a user profile
    /// </summary>
    /// <param name="userProfileId">The user profile ID</param>
    /// <returns>List of earned badges</returns>
    Task<List<UserBadge>> GetUserBadgesAsync(string userProfileId);

    /// <summary>
    /// Get badges for a specific axis for a user profile
    /// </summary>
    /// <param name="userProfileId">The user profile ID</param>
    /// <param name="axis">The compass axis</param>
    /// <returns>List of badges for the axis</returns>
    Task<List<UserBadge>> GetUserBadgesForAxisAsync(string userProfileId, string axis);

    /// <summary>
    /// Check if a user has earned a specific badge
    /// </summary>
    /// <param name="userProfileId">The user profile ID</param>
    /// <param name="badgeConfigurationId">The badge configuration ID</param>
    /// <returns>True if badge has been earned</returns>
    Task<bool> HasUserEarnedBadgeAsync(string userProfileId, string badgeConfigurationId);

    /// <summary>
    /// Remove a badge from a user profile (admin function)
    /// </summary>
    /// <param name="userProfileId">The user profile ID</param>
    /// <param name="badgeId">The badge ID to remove</param>
    /// <returns>True if badge was removed</returns>
    Task<bool> RemoveBadgeAsync(string userProfileId, string badgeId);

    /// <summary>
    /// Get badge statistics for a user profile
    /// </summary>
    /// <param name="userProfileId">The user profile ID</param>
    /// <returns>Badge statistics</returns>
    Task<Dictionary<string, int>> GetBadgeStatisticsAsync(string userProfileId);
}
