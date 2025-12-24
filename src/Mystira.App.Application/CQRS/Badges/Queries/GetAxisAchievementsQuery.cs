using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed record GetAxisAchievementsQuery(string AgeGroupId) : IQuery<List<AxisAchievementResponse>>;
