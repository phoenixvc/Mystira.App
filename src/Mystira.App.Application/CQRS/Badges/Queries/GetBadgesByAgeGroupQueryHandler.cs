using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

/// <summary>
/// Wolverine handler for GetBadgesByAgeGroupQuery.
/// Retrieves badges filtered by age group.
/// </summary>
public static class GetBadgesByAgeGroupQueryHandler
{
    /// <summary>
    /// Handles the GetBadgesByAgeGroupQuery by retrieving badges for a specific age group from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<BadgeResponse>> Handle(
        GetBadgesByAgeGroupQuery query,
        IBadgeRepository badgeRepository,
        CancellationToken ct)
    {
        var badges = await badgeRepository.GetByAgeGroupAsync(query.AgeGroupId);
        return badges
            .OrderBy(b => b.CompassAxisId)
            .ThenBy(b => b.TierOrder)
            .Select(b => new BadgeResponse
            {
                Id = b.Id,
                AgeGroupId = b.AgeGroupId,
                CompassAxisId = b.CompassAxisId,
                Tier = b.Tier,
                TierOrder = b.TierOrder,
                Title = b.Title,
                Description = b.Description,
                RequiredScore = b.RequiredScore,
                ImageId = b.ImageId
            })
            .ToList();
    }
}
