using Mystira.App.Application.Ports.Services;
using Mystira.Shared.Extensions;

namespace Mystira.App.Api.Services;

/// <summary>
/// Implementation of ICurrentUserService that uses HttpContext to access the current user's claims.
/// Uses Mystira.Shared.Extensions.ClaimsPrincipalExtensions for consistent claim extraction.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public string? GetAccountId()
    {
        return User?.GetAccountId();
    }

    /// <inheritdoc />
    public string GetRequiredAccountId()
    {
        var accountId = GetAccountId();
        if (string.IsNullOrEmpty(accountId))
        {
            throw new UnauthorizedAccessException("User is not authenticated or account ID not found");
        }
        return accountId;
    }

    /// <inheritdoc />
    public string? GetClaim(string claimType)
    {
        return User?.GetClaimValue(claimType);
    }

    /// <inheritdoc />
    public string? GetEmail()
    {
        return User?.GetEmail();
    }

    /// <inheritdoc />
    public string? GetDisplayName()
    {
        return User?.GetDisplayName();
    }
}
