using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mystira.App.Shared.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly HashSet<string> _invalidatedRefreshTokens;
    private readonly SigningCredentials _signingCredentials;
    private readonly bool _useAsymmetric;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Fail fast if JWT configuration is missing - no hardcoded fallbacks
        _issuer = _configuration["JwtSettings:Issuer"]
            ?? _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer not configured. Please provide JwtSettings:Issuer or Jwt:Issuer in configuration.");

        _audience = _configuration["JwtSettings:Audience"]
            ?? _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience not configured. Please provide JwtSettings:Audience or Jwt:Audience in configuration.");

        _invalidatedRefreshTokens = new HashSet<string>();

        // Check if asymmetric signing is configured
        var rsaPrivateKey = _configuration["JwtSettings:RsaPrivateKey"] ?? _configuration["Jwt:RsaPrivateKey"];
        _useAsymmetric = !string.IsNullOrEmpty(rsaPrivateKey);

        if (_useAsymmetric)
        {
            // Use asymmetric RS256 signing
            try
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(rsaPrivateKey!);
                var rsaSecurityKey = new RsaSecurityKey(rsa);
                _signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);
                _logger.LogInformation("JwtService initialized with RS256 asymmetric signing. Issuer: {Issuer}, Audience: {Audience}", _issuer, _audience);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load RSA private key for asymmetric signing");
                throw new InvalidOperationException(
                    "Failed to load RSA private key. Ensure JwtSettings:RsaPrivateKey or Jwt:RsaPrivateKey " +
                    "contains a valid PEM-encoded RSA private key from a secure store (Azure Key Vault, AWS Secrets Manager, etc.)", ex);
            }
        }
        else
        {
            // Fall back to symmetric HS256 signing (for backwards compatibility)
            // In production, this should be phased out in favor of asymmetric signing
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException(
                    "JWT signing key not configured. Please provide either:\n" +
                    "- JwtSettings:RsaPrivateKey or Jwt:RsaPrivateKey for asymmetric RS256 signing (recommended), OR\n" +
                    "- JwtSettings:SecretKey or Jwt:Key for symmetric HS256 signing (legacy)\n" +
                    "Keys must be loaded from secure stores (Azure Key Vault, AWS Secrets Manager, etc.). " +
                    "Never hardcode secrets in source code.");
            }

            var key = Encoding.ASCII.GetBytes(secretKey);
            _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
            _logger.LogWarning("JwtService initialized with HS256 symmetric signing. Consider migrating to RS256 asymmetric signing for better security. Issuer: {Issuer}, Audience: {Audience}", _issuer, _audience);
        }
    }

    public string GenerateAccessToken(string userId, string email, string displayName, string? role = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", displayName),
            new Claim("account_id", userId)
        };

        // Add role claim if provided
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        else
        {
            // Default role is Guest
            claims.Add(new Claim(ClaimTypes.Role, "Guest"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(6),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = _signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random refresh token
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = GetValidationParameters(validateLifetime: true);
            tokenHandler.ValidateToken(token, parameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateRefreshToken(string token, string storedRefreshToken)
    {
        // Basic validation - ensure token matches stored value and hasn't been invalidated
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedRefreshToken))
        {
            return false;
        }

        if (!_invalidatedRefreshTokens.Contains(storedRefreshToken) && token == storedRefreshToken)
        {
            return true;
        }

        return false;
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }

    public (bool IsValid, string? UserId) ValidateAndExtractUserId(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = GetValidationParameters(validateLifetime: true);
            var principal = tokenHandler.ValidateToken(token, parameters, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
            return (true, userId);
        }
        catch
        {
            return (false, null);
        }
    }

    public (bool IsValid, string? UserId) ExtractUserIdIgnoringExpiry(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = GetValidationParameters(validateLifetime: false);
            var principal = tokenHandler.ValidateToken(token, parameters, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
            return (true, userId);
        }
        catch
        {
            return (false, null);
        }
    }

    private TokenValidationParameters GetValidationParameters(bool validateLifetime)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        if (_useAsymmetric)
        {
            var rsaPublicKey = _configuration["JwtSettings:RsaPublicKey"] ?? _configuration["Jwt:RsaPublicKey"];
            if (string.IsNullOrEmpty(rsaPublicKey))
            {
                throw new InvalidOperationException("RSA public key not configured for token validation. Provide JwtSettings:RsaPublicKey or Jwt:RsaPublicKey.");
            }
            var rsa = RSA.Create();
            rsa.ImportFromPem(rsaPublicKey);
            parameters.IssuerSigningKey = new RsaSecurityKey(rsa);
        }
        else
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? _configuration["Jwt:Key"]
                            ?? throw new InvalidOperationException("JWT SecretKey/Jwt:Key not configured for HS256 validation.");
            parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
        }

        return parameters;
    }
}
