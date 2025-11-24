using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

/// <summary>
/// Query to retrieve a badge configuration by ID
/// </summary>
public record GetBadgeConfigurationQuery(string BadgeId) : IQuery<BadgeConfiguration?>;
