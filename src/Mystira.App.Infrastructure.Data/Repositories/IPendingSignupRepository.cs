using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for PendingSignup entity with domain-specific queries
/// </summary>
public interface IPendingSignupRepository : IRepository<PendingSignup>
{
    Task<PendingSignup?> GetByEmailAndCodeAsync(string email, string code);
    Task<PendingSignup?> GetActiveByEmailAsync(string email);
    Task<IEnumerable<PendingSignup>> GetExpiredAsync();
}

