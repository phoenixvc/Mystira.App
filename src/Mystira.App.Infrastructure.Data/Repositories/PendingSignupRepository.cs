using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for PendingSignup entity
/// </summary>
public class PendingSignupRepository : Repository<PendingSignup>, IPendingSignupRepository
{
    public PendingSignupRepository(DbContext context) : base(context)
    {
    }

    public async Task<PendingSignup?> GetByEmailAndCodeAsync(string email, string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Email == email && p.Code == code && !p.IsUsed);
    }

    public async Task<PendingSignup?> GetActiveByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Email == email && !p.IsUsed && p.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<IEnumerable<PendingSignup>> GetExpiredAsync()
    {
        return await _dbSet
            .Where(p => p.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
    }
}

