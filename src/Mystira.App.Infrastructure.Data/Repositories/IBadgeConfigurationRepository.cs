using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for BadgeConfiguration entity with domain-specific queries
/// </summary>
public interface IBadgeConfigurationRepository : IRepository<BadgeConfiguration>
{
    Task<IEnumerable<BadgeConfiguration>> GetByAxisAsync(string axis);
}

