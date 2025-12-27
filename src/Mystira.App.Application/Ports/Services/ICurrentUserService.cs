namespace Mystira.App.Application.Ports.Services;

/// <summary>
/// Service for accessing the current authenticated user's information.
/// Implementation should use Mystira.Shared.Extensions.ClaimsPrincipalExtensions.
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
    /// Gets the current user's email address
    /// </summary>
    string? GetEmail();

    /// <summary>
    /// Gets the current user's display name
    /// </summary>
    string? GetDisplayName();

    /// <summary>
    /// Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
