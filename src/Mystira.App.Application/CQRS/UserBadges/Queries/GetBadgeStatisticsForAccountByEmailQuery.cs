using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Query to get badge statistics for all profiles in an account (by email).
/// Orchestrates account lookup and statistics aggregation.
/// </summary>
public record GetBadgeStatisticsForAccountByEmailQuery(string Email)
    : IQuery<Dictionary<string, int>>;
