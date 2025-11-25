using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

/// <summary>
/// Query to retrieve badge configurations by compass axis
/// </summary>
public record GetBadgeConfigurationsByAxisQuery(string Axis) : IQuery<List<BadgeConfiguration>>;
