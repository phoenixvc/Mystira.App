using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

/// <summary>
/// Query to get all badges for a user profile on a specific compass axis.
/// </summary>
public record GetUserBadgesForAxisQuery(string UserProfileId, string Axis)
    : IQuery<List<UserBadge>>;
