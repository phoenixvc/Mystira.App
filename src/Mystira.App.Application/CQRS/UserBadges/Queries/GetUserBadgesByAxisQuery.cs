using Mystira.Shared.CQRS;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

public record GetUserBadgesByAxisQuery(string UserProfileId, string Axis)
    : IQuery<List<UserBadge>>;

public sealed class GetUserBadgesByAxisQueryHandler
    : IQueryHandler<GetUserBadgesByAxisQuery, List<UserBadge>>
{
    private readonly IUserBadgeRepository _userBadgeRepository;

    public GetUserBadgesByAxisQueryHandler(IUserBadgeRepository userBadgeRepository)
    {
        _userBadgeRepository = userBadgeRepository;
    }

    public async Task<List<UserBadge>> Handle(GetUserBadgesByAxisQuery request, CancellationToken cancellationToken)
    {
        var badges = await _userBadgeRepository.GetByUserProfileIdAsync(request.UserProfileId);
        return badges
            .Where(b => string.Equals(b.Axis, request.Axis, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.EarnedAt)
            .ToList();
    }
}
