using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Account entity with domain-specific queries
/// </summary>
public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(DbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());
    }

    public async Task<Account?> GetByAuth0UserIdAsync(string auth0UserId)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Auth0UserId == auth0UserId);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbSet.AnyAsync(a => a.Email.ToLower() == email.ToLower());
    }
}

