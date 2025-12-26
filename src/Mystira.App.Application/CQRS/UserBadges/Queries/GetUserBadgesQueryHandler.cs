using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Wolverine handler for GetUserBadgesQuery.
/// Retrieves all badges for a user profile.
/// </summary>
public static class GetUserBadgesQueryHandler
{
    /// <summary>
    /// Handles the GetUserBadgesQuery by retrieving user badges from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<UserBadge>> Handle(
        GetUserBadgesQuery query,
        IUserBadgeRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        var spec = new UserBadgesByProfileSpec(query.UserProfileId);
        var badges = await repository.ListAsync(spec);

        logger.LogDebug("Retrieved {Count} badges for user profile {UserProfileId}",
            badges.Count(), query.UserProfileId);

        return badges.ToList();
    }
}
