using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

public record AwardBadgeCommand(AwardBadgeRequest Request) : ICommand<UserBadge>;
