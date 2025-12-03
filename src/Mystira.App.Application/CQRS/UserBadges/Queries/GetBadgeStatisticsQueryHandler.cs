using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Handler for retrieving badge statistics for a user profile.
/// Groups badges by axis and counts them.
/// </summary>
public class GetBadgeStatisticsQueryHandler
    : IQueryHandler<GetBadgeStatisticsQuery, Dictionary<string, int>>
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<GetBadgeStatisticsQueryHandler> _logger;

    public GetBadgeStatisticsQueryHandler(
        IUserBadgeRepository repository,
        ILogger<GetBadgeStatisticsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Dictionary<string, int>> Handle(
        GetBadgeStatisticsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badge statistics for user {UserProfileId}", query.UserProfileId);

        var badges = await _repository.GetByUserProfileIdAsync(query.UserProfileId);

        var statistics = badges
            .Where(b => !string.IsNullOrEmpty(b.Axis))
            .GroupBy(b => b.Axis)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogInformation("Found badge statistics for {AxisCount} axes", statistics.Count);
        return statistics;
    }
}
