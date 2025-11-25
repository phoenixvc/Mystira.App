using MediatR;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Handler for retrieving all badges for all profiles in an account.
/// Coordinates account lookup, profile retrieval, and badge aggregation.
/// </summary>
public class GetBadgesForAccountByEmailQueryHandler
    : IQueryHandler<GetBadgesForAccountByEmailQuery, List<UserBadge>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetBadgesForAccountByEmailQueryHandler> _logger;

    public GetBadgesForAccountByEmailQueryHandler(
        IMediator mediator,
        ILogger<GetBadgesForAccountByEmailQueryHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<List<UserBadge>> Handle(
        GetBadgesForAccountByEmailQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badges for account with email {Email}", query.Email);

        // Get account by email
        var accountQuery = new GetAccountByEmailQuery(query.Email);
        var account = await _mediator.Send(accountQuery, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Account not found for email {Email}", query.Email);
            return new List<UserBadge>();
        }

        // Get profiles for account
        var profilesQuery = new GetProfilesByAccountQuery(account.Id);
        var profiles = await _mediator.Send(profilesQuery, cancellationToken);

        // Get badges for all profiles in parallel
        var badgeTasks = profiles
            .Select(profile => _mediator.Send(new GetUserBadgesQuery(profile.Id), cancellationToken))
            .ToList();

        var badgeLists = await Task.WhenAll(badgeTasks);
        var allBadges = badgeLists.SelectMany(b => b).ToList();

        _logger.LogInformation("Found {Count} total badges for account {Email}",
            allBadges.Count, query.Email);

        return allBadges;
    }
}
