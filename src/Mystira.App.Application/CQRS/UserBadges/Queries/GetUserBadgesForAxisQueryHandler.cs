using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Wolverine handler for GetUserBadgesForAxisQuery.
/// Retrieves badges for a specific compass axis.
/// </summary>
public static class GetUserBadgesForAxisQueryHandler
{
    /// <summary>
    /// Handles the GetUserBadgesForAxisQuery by retrieving badges for a specific axis from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<UserBadge>> Handle(
        GetUserBadgesForAxisQuery query,
        IUserBadgeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting badges for user {UserProfileId} on axis {Axis}",
            query.UserProfileId, query.Axis);

        var badges = await repository.GetByUserProfileIdAsync(query.UserProfileId);
        var filteredBadges = badges
            .Where(b => b.Axis?.Equals(query.Axis, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        logger.LogInformation("Found {Count} badges for axis {Axis}", filteredBadges.Count, query.Axis);
        return filteredBadges;
    }
}
