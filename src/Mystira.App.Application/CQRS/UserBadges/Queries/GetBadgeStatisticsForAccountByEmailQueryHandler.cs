using Wolverine;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Wolverine handler for GetBadgeStatisticsForAccountByEmailQuery.
/// Retrieves badge statistics for all profiles in an account.
/// Coordinates account lookup, profile retrieval, and statistics aggregation.
/// </summary>
public static class GetBadgeStatisticsForAccountByEmailQueryHandler
{
    /// <summary>
    /// Handles the GetBadgeStatisticsForAccountByEmailQuery by aggregating badge statistics across all profiles in an account.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Dictionary<string, int>> Handle(
        GetBadgeStatisticsForAccountByEmailQuery query,
        IMessageBus messageBus,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting badge statistics for account with email {Email}", query.Email);

        // Get account by email
        var accountQuery = new GetAccountByEmailQuery(query.Email);
        var account = await messageBus.InvokeAsync<Account?>(accountQuery, ct);

        if (account == null)
        {
            logger.LogWarning("Account not found for email {Email}", query.Email);
            return new Dictionary<string, int>();
        }

        // Get profiles for account
        var profilesQuery = new GetProfilesByAccountQuery(account.Id);
        var profiles = await messageBus.InvokeAsync<List<Domain.Models.UserProfile>>(profilesQuery, ct);

        // Aggregate statistics from all profiles
        var combinedStatistics = new Dictionary<string, int>();
        foreach (var profile in profiles)
        {
            var statsQuery = new GetBadgeStatisticsQuery(profile.Id);
            var profileStats = await messageBus.InvokeAsync<Dictionary<string, int>>(statsQuery, ct);

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

        logger.LogInformation("Aggregated statistics for {AxisCount} axes across {ProfileCount} profiles",
            combinedStatistics.Count, profiles.Count);

        return combinedStatistics;
    }
}
