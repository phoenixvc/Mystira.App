using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for Account entity with domain-specific queries
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByExternalUserIdAsync(string externalUserId);
    Task<bool> ExistsByEmailAsync(string email);
}

