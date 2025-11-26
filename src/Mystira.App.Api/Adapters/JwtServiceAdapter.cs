using Mystira.App.Application.Ports.Auth;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Adapters;

/// <summary>
/// Adapter that adapts API.Services.IJwtService to Application.Ports.Auth.IJwtService
/// </summary>
public class JwtServiceAdapter : Application.Ports.Auth.IJwtService
{
    private readonly Services.IJwtService _apiService;

    public JwtServiceAdapter(Services.IJwtService apiService)
    {
        _apiService = apiService;
    }

    public string GenerateAccessToken(string userId, string email, string displayName, string? role = null)
    {
        return _apiService.GenerateAccessToken(userId, email, displayName, role);
    }

    public string GenerateRefreshToken()
    {
        return _apiService.GenerateRefreshToken();
    }

    public bool ValidateToken(string token)
    {
        return _apiService.ValidateToken(token);
    }

    public bool ValidateRefreshToken(string token, string storedRefreshToken)
    {
        return _apiService.ValidateRefreshToken(token, storedRefreshToken);
    }

    public string? GetUserIdFromToken(string token)
    {
        return _apiService.GetUserIdFromToken(token);
    }

    public (bool IsValid, string? UserId) ValidateAndExtractUserId(string token)
    {
        return _apiService.ValidateAndExtractUserId(token);
    }
}

