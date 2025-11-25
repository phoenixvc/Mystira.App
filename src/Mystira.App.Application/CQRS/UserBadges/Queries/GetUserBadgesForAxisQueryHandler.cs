using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Handler for retrieving badges for a specific compass axis.
/// </summary>
public class GetUserBadgesForAxisQueryHandler
    : IQueryHandler<GetUserBadgesForAxisQuery, List<UserBadge>>
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<GetUserBadgesForAxisQueryHandler> _logger;

    public GetUserBadgesForAxisQueryHandler(
        IUserBadgeRepository repository,
        ILogger<GetUserBadgesForAxisQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<UserBadge>> Handle(
        GetUserBadgesForAxisQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badges for user {UserProfileId} on axis {Axis}",
            query.UserProfileId, query.Axis);

        var badges = await _repository.GetByUserProfileIdAsync(query.UserProfileId);
        var filteredBadges = badges
            .Where(b => b.BadgeConfiguration?.Axis?.Equals(query.Axis, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation("Found {Count} badges for axis {Axis}", filteredBadges.Count, query.Axis);
        return filteredBadges;
    }
}
