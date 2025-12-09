using MediatR;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Application.CQRS.UserProfiles.Queries;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Handler for retrieving badge statistics for all profiles in an account.
/// Coordinates account lookup, profile retrieval, and statistics aggregation.
/// </summary>
public class GetBadgeStatisticsForAccountByEmailQueryHandler
    : IQueryHandler<GetBadgeStatisticsForAccountByEmailQuery, Dictionary<string, int>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetBadgeStatisticsForAccountByEmailQueryHandler> _logger;

    public GetBadgeStatisticsForAccountByEmailQueryHandler(
        IMediator mediator,
        ILogger<GetBadgeStatisticsForAccountByEmailQueryHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Dictionary<string, int>> Handle(
        GetBadgeStatisticsForAccountByEmailQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badge statistics for account with email {Email}", query.Email);

        // Get account by email
        var accountQuery = new GetAccountByEmailQuery(query.Email);
        var account = await _mediator.Send(accountQuery, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Account not found for email {Email}", query.Email);
            return new Dictionary<string, int>();
        }

        // Get profiles for account
        var profilesQuery = new GetProfilesByAccountQuery(account.Id);
        var profiles = await _mediator.Send(profilesQuery, cancellationToken);

        // Aggregate statistics from all profiles
        var combinedStatistics = new Dictionary<string, int>();
        foreach (var profile in profiles)
        {
            var statsQuery = new GetBadgeStatisticsQuery(profile.Id);
            var profileStats = await _mediator.Send(statsQuery, cancellationToken);

            foreach (var stat in profileStats)
            {
                if (combinedStatistics.TryGetValue(stat.Key, out var existingValue))
                {
                    combinedStatistics[stat.Key] = existingValue + stat.Value;
                }
                else
                {
                    combinedStatistics[stat.Key] = stat.Value;
                }
            }
        }

        _logger.LogInformation("Aggregated statistics for {AxisCount} axes across {ProfileCount} profiles",
            combinedStatistics.Count, profiles.Count);

        return combinedStatistics;
    }
}
