using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

public class GetUserBadgesQueryHandler : IQueryHandler<GetUserBadgesQuery, List<UserBadge>>
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<GetUserBadgesQueryHandler> _logger;

    public GetUserBadgesQueryHandler(
        IUserBadgeRepository repository,
        ILogger<GetUserBadgesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<UserBadge>> Handle(GetUserBadgesQuery request, CancellationToken cancellationToken)
    {
        var spec = new UserBadgesByProfileSpecification(request.UserProfileId);
        var badges = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} badges for user profile {UserProfileId}",
            badges.Count(), request.UserProfileId);

        return badges.ToList();
    }
}
