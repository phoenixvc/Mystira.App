using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for UserProfile entity with domain-specific queries
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile, string>
{
    Task<IEnumerable<UserProfile>> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<UserProfile>> GetGuestProfilesAsync();
    Task<IEnumerable<UserProfile>> GetNonGuestProfilesAsync();
}

