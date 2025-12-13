using Mystira.App.Contracts.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed record GetBadgesByAgeGroupQuery(string AgeGroupId) : IQuery<List<BadgeResponse>>;
