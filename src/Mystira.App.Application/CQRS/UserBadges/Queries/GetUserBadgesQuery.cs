using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

public record GetUserBadgesQuery(string UserProfileId) : IQuery<List<UserBadge>>;
