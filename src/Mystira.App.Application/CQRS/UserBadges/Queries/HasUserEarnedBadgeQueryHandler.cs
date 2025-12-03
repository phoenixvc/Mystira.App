using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Handler for checking if a user has earned a specific badge.
/// </summary>
public class HasUserEarnedBadgeQueryHandler : IQueryHandler<HasUserEarnedBadgeQuery, bool>
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<HasUserEarnedBadgeQueryHandler> _logger;

    public HasUserEarnedBadgeQueryHandler(
        IUserBadgeRepository repository,
        ILogger<HasUserEarnedBadgeQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        HasUserEarnedBadgeQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking if user {UserProfileId} has earned badge {BadgeId}",
            query.UserProfileId, query.BadgeConfigurationId);

        var badges = await _repository.GetByUserProfileIdAsync(query.UserProfileId);
        var hasEarned = badges.Any(b => b.BadgeConfigurationId == query.BadgeConfigurationId);

        _logger.LogInformation("User {UserProfileId} {Status} badge {BadgeId}",
            query.UserProfileId, hasEarned ? "has earned" : "has not earned", query.BadgeConfigurationId);

        return hasEarned;
    }
}
