namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Query to check if a user has earned a specific badge.
/// </summary>
public record HasUserEarnedBadgeQuery(string UserProfileId, string BadgeConfigurationId)
    : IQuery<bool>;
