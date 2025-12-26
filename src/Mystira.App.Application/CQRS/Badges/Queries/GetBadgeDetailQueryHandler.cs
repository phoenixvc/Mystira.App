using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

/// <summary>
/// Wolverine handler for GetBadgeDetailQuery.
/// Retrieves detailed information for a specific badge.
/// </summary>
public static class GetBadgeDetailQueryHandler
{
    /// <summary>
    /// Handles the GetBadgeDetailQuery by retrieving badge details from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<BadgeResponse?> Handle(
        GetBadgeDetailQuery query,
        IBadgeRepository badgeRepository,
        CancellationToken ct)
    {
        var badge = await badgeRepository.GetByIdAsync(query.BadgeId);
        if (badge == null) return null;

        return new BadgeResponse
        {
            Id = badge.Id,
            AgeGroupId = badge.AgeGroupId,
            CompassAxisId = badge.CompassAxisId,
            Tier = badge.Tier,
            TierOrder = badge.TierOrder,
            Title = badge.Title,
            Description = badge.Description,
            RequiredScore = badge.RequiredScore,
            ImageId = badge.ImageId
        };
    }
}
