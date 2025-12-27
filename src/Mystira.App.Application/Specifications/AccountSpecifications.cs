using Ardalis.Specification;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Specifications;

/// <summary>
/// Specification to find an account by email address (case-insensitive).
/// Returns a single result.
/// </summary>
public sealed class AccountByEmailSpec : SingleEntitySpecification<Account>
{
    public AccountByEmailSpec(string email)
    {
        Query.Where(a => a.Email.ToLower() == email.ToLower());
    }
}

/// <summary>
/// Specification to find an account by external user ID (Entra External ID).
/// Returns a single result.
/// </summary>
public sealed class AccountByExternalUserIdSpec : SingleEntitySpecification<Account>
{
    public AccountByExternalUserIdSpec(string externalUserId)
    {
        Query.Where(a => a.ExternalUserId == externalUserId);
    }
}

/// <summary>
/// Specification to find an account by ID.
/// Returns a single result.
/// </summary>
public sealed class AccountByIdSpec : SingleEntitySpecification<Account>
{
    public AccountByIdSpec(string id)
    {
        Query.Where(a => a.Id == id);
    }
}

/// <summary>
/// Specification to get all active accounts.
/// </summary>
/// <summary>
/// Specification to get active accounts (with active subscriptions).
/// </summary>
public sealed class ActiveAccountsSpec : BaseEntitySpecification<Account>
{
    public ActiveAccountsSpec()
    {
        Query.Where(a => a.Subscription.IsActive)
             .OrderByDescending(a => a.CreatedAt);
    }
}

/// <summary>
/// Specification to search accounts by email pattern.
/// </summary>
public sealed class AccountsByEmailPatternSpec : BaseEntitySpecification<Account>
{
    public AccountsByEmailPatternSpec(string emailPattern)
    {
        Query.Where(a => a.Email.ToLower().Contains(emailPattern.ToLower()))
             .OrderBy(a => a.Email);
    }
}

/// <summary>
/// Specification for paginated account list with optional search.
/// </summary>
public sealed class AccountsPaginatedSpec : BaseEntitySpecification<Account>
{
    public AccountsPaginatedSpec(int skip, int take, string? searchTerm = null)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            Query.Where(a => a.Email.ToLower().Contains(searchTerm.ToLower()));
        }

        Query.OrderBy(a => a.Email)
             .Skip(skip)
             .Take(take);
    }
}
