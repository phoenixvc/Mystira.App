namespace Mystira.App.Application.Ports.Auth;

/// <summary>
/// Port interface for JWT token operations.
/// Implementations handle token generation and validation.
/// </summary>
public interface IJwtService
{
    string GenerateAccessToken(string userId, string email, string displayName, string? role = null);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    bool ValidateRefreshToken(string token, string storedRefreshToken);
    string? GetUserIdFromToken(string token);
    (bool IsValid, string? UserId) ValidateAndExtractUserId(string token);
}
