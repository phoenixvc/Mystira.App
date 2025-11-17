using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mystira.App.Api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly HashSet<string> _invalidatedRefreshTokens;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = _configuration["JwtSettings:Issuer"] ?? "MystiraAPI";
        _audience = _configuration["JwtSettings:Audience"] ?? "MystiraPWA";
        _invalidatedRefreshTokens = new HashSet<string>();
    }

    public string GenerateAccessToken(string userId, string email, string displayName)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("sub", userId),
                new Claim("email", email),
                new Claim("name", displayName)
            }),
            Expires = DateTime.UtcNow.AddHours(6), // Access token expires in 30 minutes
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        
        _logger.LogInformation("Generated access token for user: {UserId}", userId);
        return tokenString;
    }

    public string GenerateRefreshToken()
    {
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _logger.LogDebug("Generated refresh token");
        return refreshToken;
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    public bool ValidateRefreshToken(string token, string storedRefreshToken)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedRefreshToken))
        {
            return false;
        }

        // Check if the refresh token has been invalidated
        if (_invalidatedRefreshTokens.Contains(storedRefreshToken))
        {
            _logger.LogWarning("Refresh token has been invalidated");
            return false;
        }

        // For refresh tokens, we just check if they match the stored one
        // In a production environment, you might want to add expiration logic here too
        return token == storedRefreshToken;
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier || x.Type == "sub")?.Value;
            
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return null;
        }
    }

    public (bool IsValid, string? UserId) ValidateAndExtractUserId(string token)
    {
        try
        {
            if (!ValidateToken(token))
            {
                return (false, null);
            }

            var userId = GetUserIdFromToken(token);
            return (true, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating and extracting user ID from token");
            return (false, null);
        }
    }

    public void InvalidateRefreshToken(string refreshToken)
    {
        _invalidatedRefreshTokens.Add(refreshToken);
        _logger.LogInformation("Refresh token invalidated");
    }
}