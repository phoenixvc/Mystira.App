using Ardalis.Specification;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for Account entity with domain-specific queries
/// </summary>
public interface IAccountRepository : IRepository<Account, string>, IRepositoryBase<Account>
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByExternalUserIdAsync(string externalUserId);
    Task<bool> ExistsByEmailAsync(string email);
}
