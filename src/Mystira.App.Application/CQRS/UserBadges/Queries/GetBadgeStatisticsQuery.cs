namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Query to get badge statistics for a user profile.
/// Returns count of badges per axis/category.
/// </summary>
public record GetBadgeStatisticsQuery(string UserProfileId)
    : IQuery<Dictionary<string, int>>;
