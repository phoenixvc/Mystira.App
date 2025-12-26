using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Wolverine handler for GetBadgeStatisticsQuery.
/// Retrieves badge statistics for a user profile.
/// Groups badges by axis and counts them.
/// </summary>
public static class GetBadgeStatisticsQueryHandler
{
    /// <summary>
    /// Handles the GetBadgeStatisticsQuery by retrieving and aggregating badge statistics from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Dictionary<string, int>> Handle(
        GetBadgeStatisticsQuery query,
        IUserBadgeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting badge statistics for user {UserProfileId}", query.UserProfileId);

        var badges = await repository.GetByUserProfileIdAsync(query.UserProfileId);

        var statistics = badges
            .Where(b => !string.IsNullOrEmpty(b.Axis))
            .GroupBy(b => b.Axis)
            .ToDictionary(g => g.Key, g => g.Count());

        logger.LogInformation("Found badge statistics for {AxisCount} axes", statistics.Count);
        return statistics;
    }
}
