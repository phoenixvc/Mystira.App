using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed class GetBadgeDetailQueryHandler : IQueryHandler<GetBadgeDetailQuery, BadgeResponse?>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgeDetailQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<BadgeResponse?> Handle(GetBadgeDetailQuery request, CancellationToken cancellationToken)
    {
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId);
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
