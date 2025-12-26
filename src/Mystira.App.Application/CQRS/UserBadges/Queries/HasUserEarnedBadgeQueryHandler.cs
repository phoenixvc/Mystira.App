using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Wolverine handler for HasUserEarnedBadgeQuery.
/// Checks if a user has earned a specific badge.
/// </summary>
public static class HasUserEarnedBadgeQueryHandler
{
    /// <summary>
    /// Handles the HasUserEarnedBadgeQuery by checking if a user has earned a specific badge.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        HasUserEarnedBadgeQuery query,
        IUserBadgeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Checking if user {UserProfileId} has earned badge {BadgeId}",
            query.UserProfileId, query.BadgeConfigurationId);

        var badges = await repository.GetByUserProfileIdAsync(query.UserProfileId);
        var hasEarned = badges.Any(b => b.BadgeConfigurationId == query.BadgeConfigurationId);

        logger.LogInformation("User {UserProfileId} {Status} badge {BadgeId}",
            query.UserProfileId, hasEarned ? "has earned" : "has not earned", query.BadgeConfigurationId);

        return hasEarned;
    }
}
