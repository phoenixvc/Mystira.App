namespace Mystira.App.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(string userId, string email, string displayName, string? role = null);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    bool ValidateRefreshToken(string token, string storedRefreshToken);
    string? GetUserIdFromToken(string token);
    (bool IsValid, string? UserId) ValidateAndExtractUserId(string token);

    /// <summary>
    /// Extracts user ID from a token without validating its lifetime.
    /// Used for refresh token flow where the access token may be expired.
    /// Still validates signature, issuer, and audience.
    /// </summary>
    (bool IsValid, string? UserId) ExtractUserIdIgnoringExpiry(string token);
}
