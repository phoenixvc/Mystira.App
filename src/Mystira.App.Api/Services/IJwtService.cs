namespace Mystira.App.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(string userId, string email, string displayName);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    bool ValidateRefreshToken(string token, string storedRefreshToken);
    string? GetUserIdFromToken(string token);
    (bool IsValid, string? UserId) ValidateAndExtractUserId(string token);
}