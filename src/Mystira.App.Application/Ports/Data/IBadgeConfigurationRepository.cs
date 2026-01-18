using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for BadgeConfiguration entity with domain-specific queries
/// </summary>
public interface IBadgeConfigurationRepository : IRepository<BadgeConfiguration, string>
{
    Task<IEnumerable<BadgeConfiguration>> GetByAxisAsync(string axis);
}
