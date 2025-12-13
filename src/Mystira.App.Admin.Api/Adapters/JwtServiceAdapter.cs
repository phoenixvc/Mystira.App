using Mystira.App.Shared.Services;

namespace Mystira.App.Admin.Api.Adapters;

/// <summary>
/// Adapter that adapts Shared.Services.IJwtService to Application.Ports.Auth.IJwtService for Admin API
/// </summary>
public class JwtServiceAdapter : Mystira.App.Application.Ports.Auth.IJwtService
{
    private readonly IJwtService _jwtService;

    public JwtServiceAdapter(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public string GenerateAccessToken(string userId, string email, string displayName, string? role = null)
    {
        return _jwtService.GenerateAccessToken(userId, email, displayName, role);
    }

    public string GenerateRefreshToken()
    {
        return _jwtService.GenerateRefreshToken();
    }

    public bool ValidateToken(string token)
    {
        return _jwtService.ValidateToken(token);
    }

    public bool ValidateRefreshToken(string token, string storedRefreshToken)
    {
        return _jwtService.ValidateRefreshToken(token, storedRefreshToken);
    }

    public string? GetUserIdFromToken(string token)
    {
        return _jwtService.GetUserIdFromToken(token);
    }

    public (bool IsValid, string? UserId) ValidateAndExtractUserId(string token)
    {
        return _jwtService.ValidateAndExtractUserId(token);
    }

    public (bool IsValid, string? UserId) ExtractUserIdIgnoringExpiry(string token)
    {
        return _jwtService.ExtractUserIdIgnoringExpiry(token);
    }
}
