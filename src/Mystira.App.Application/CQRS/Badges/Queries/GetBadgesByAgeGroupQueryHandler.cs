using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed class GetBadgesByAgeGroupQueryHandler : IQueryHandler<GetBadgesByAgeGroupQuery, List<BadgeResponse>>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgesByAgeGroupQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<List<BadgeResponse>> Handle(GetBadgesByAgeGroupQuery request, CancellationToken cancellationToken)
    {
        var badges = await _badgeRepository.GetByAgeGroupAsync(request.AgeGroupId);
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
