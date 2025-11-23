using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for UserBadge entity with domain-specific queries
/// </summary>
public interface IUserBadgeRepository : IRepository<UserBadge>
{
    Task<IEnumerable<UserBadge>> GetByUserProfileIdAsync(string userProfileId);
    Task<UserBadge?> GetByUserProfileIdAndBadgeConfigIdAsync(string userProfileId, string badgeConfigurationId);
    Task<IEnumerable<UserBadge>> GetByGameSessionIdAsync(string gameSessionId);
    Task<IEnumerable<UserBadge>> GetByScenarioIdAsync(string scenarioId);
    Task<IEnumerable<UserBadge>> GetByUserProfileIdAndAxisAsync(string userProfileId, string axis);
}

