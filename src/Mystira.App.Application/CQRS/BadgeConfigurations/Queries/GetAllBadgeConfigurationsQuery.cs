using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

/// <summary>
/// Query to retrieve all badge configurations
/// </summary>
public record GetAllBadgeConfigurationsQuery() : IQuery<List<BadgeConfiguration>>;
