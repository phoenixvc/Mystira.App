using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for UserProfile entity with domain-specific queries
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile>
{
    Task<UserProfile?> GetByNameAsync(string name);
    Task<IEnumerable<UserProfile>> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<UserProfile>> GetGuestProfilesAsync();
    Task<IEnumerable<UserProfile>> GetNonGuestProfilesAsync();
    Task<bool> ExistsByNameAsync(string name);
}

