using System.Security.Claims;

namespace Mystira.App.Application.Ports.Services;

/// <summary>
/// Service for accessing the current authenticated user's information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's account ID from claims
    /// </summary>
    /// <returns>The account ID, or null if not authenticated</returns>
    string? GetAccountId();

    /// <summary>
    /// Gets the current user's account ID, throwing if not authenticated
    /// </summary>
    /// <returns>The account ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
    string GetRequiredAccountId();

    /// <summary>
    /// Gets a specific claim value from the current user
    /// </summary>
    /// <param name="claimType">The claim type to retrieve</param>
    /// <returns>The claim value, or null if not found</returns>
    string? GetClaim(string claimType);

    /// <summary>
    /// Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Extension methods for ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the account ID from common claim types
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The account ID, or null if not found</returns>
    public static string? GetAccountId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("account_id")?.Value;
    }

    /// <summary>
    /// Gets the account ID, throwing if not authenticated
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The account ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when account ID is not found</exception>
    public static string GetRequiredAccountId(this ClaimsPrincipal principal)
    {
        return principal.GetAccountId()
            ?? throw new UnauthorizedAccessException("User is not authenticated or account ID not found");
    }
}
