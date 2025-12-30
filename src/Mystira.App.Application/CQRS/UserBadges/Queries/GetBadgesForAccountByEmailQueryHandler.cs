using Wolverine;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Wolverine handler for GetBadgesForAccountByEmailQuery.
/// Retrieves all badges for all profiles in an account.
/// Coordinates account lookup, profile retrieval, and badge aggregation.
/// </summary>
public static class GetBadgesForAccountByEmailQueryHandler
{
    /// <summary>
    /// Handles the GetBadgesForAccountByEmailQuery by aggregating badges across all profiles in an account.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<UserBadge>> Handle(
        GetBadgesForAccountByEmailQuery query,
        IMessageBus messageBus,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting badges for account with email {Email}", query.Email);

        // Get account by email
        var accountQuery = new GetAccountByEmailQuery(query.Email);
        var account = await messageBus.InvokeAsync<Account?>(accountQuery, ct);

        if (account == null)
        {
            logger.LogWarning("Account not found for email {Email}", query.Email);
            return new List<UserBadge>();
        }

        // Get profiles for account
        var profilesQuery = new GetProfilesByAccountQuery(account.Id);
        var profiles = await messageBus.InvokeAsync<List<UserProfile>>(profilesQuery, ct);

        // Get badges for all profiles in parallel
        var badgeTasks = profiles.Select(profile =>
            messageBus.InvokeAsync<List<UserBadge>>(new GetUserBadgesQuery(profile.Id), ct));
        var badgeResults = await Task.WhenAll(badgeTasks);
        var allBadges = badgeResults.SelectMany(b => b).ToList();

        logger.LogInformation("Found {Count} total badges for account {Email}",
            allBadges.Count, query.Email);

        return allBadges;
    }
}
