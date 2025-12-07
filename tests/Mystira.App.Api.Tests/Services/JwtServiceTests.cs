using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Mystira.App.Api.Services;
using Xunit;

namespace Mystira.App.Api.Tests.Services;

public class JwtServiceTests
{
    private readonly Mock<ILogger<JwtService>> _mockLogger;
    private readonly IConfiguration _configuration;
    
    // Test-only secret key - DO NOT use in production
    // This is a long test secret for HS256 signing in unit tests only
    private const string TestSecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHS256Signing123456789";

    public JwtServiceTests()
    {
        _mockLogger = new Mock<ILogger<JwtService>>();
        
        // Create test configuration with symmetric key for testing
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "MystiraAPI",
                ["JwtSettings:Audience"] = "MystiraPWA",
                ["JwtSettings:SecretKey"] = TestSecretKey
            })
            .Build();
    }

    [Fact]
    public void GenerateAccessToken_CreatesValidToken_WithRequiredClaims()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";

        // Act
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Decode and verify token claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("MystiraAPI");
        jwtToken.Audiences.Should().Contain("MystiraPWA");
        
        // Verify standard claims are present
        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == "sub" && c.Value == userId);
        claims.Should().Contain(c => c.Type == "email" && c.Value == email);
        claims.Should().Contain(c => c.Type == "name" && c.Value == displayName);
        claims.Should().Contain(c => c.Type == "account_id" && c.Value == userId);
    }

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaim_WithCorrectValue()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var role = "Admin";

        // Act
        var token = service.GenerateAccessToken(userId, email, displayName, role);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var claims = jwtToken.Claims.ToList();
        
        // Verify role claim is present (JWT serialization maps ClaimTypes.Role to "role")
        var roleClaims = claims.Where(c => c.Type == "role").ToList();
        roleClaims.Should().NotBeEmpty("JWT tokens must include role claim");
        roleClaims.Should().Contain(c => c.Value == role,
            "JWT tokens should include the specified role value");
    }

    [Fact]
    public void GenerateAccessToken_DefaultsToGuestRole_WhenNoRoleProvided()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";

        // Act
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var claims = jwtToken.Claims.ToList();
        
        // Verify default Guest role is assigned
        var roleClaims = claims.Where(c => c.Type == "role").ToList();
        roleClaims.Should().NotBeEmpty();
        roleClaims.Should().Contain(c => c.Value == "Guest");
    }

    [Fact]
    public void GenerateAccessToken_SetsExpiration_ToSixHours()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";

        // Act
        var beforeGeneration = DateTime.UtcNow;
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        // Token should expire in approximately 6 hours
        var expectedExpiration = beforeGeneration.AddHours(6);
        var tokenExpiration = jwtToken.ValidTo;
        
        // Use 30-second tolerance for more precise timing validation
        tokenExpiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ValidateToken_ReturnsTrue_ForValidToken()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Act
        var isValid = service.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ReturnsFalse_ForInvalidToken()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = service.ValidateToken(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetUserIdFromToken_ExtractsUserId_FromValidToken()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Act
        var extractedUserId = service.GetUserIdFromToken(token);

        // Assert
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserIdFromToken_ReturnsNull_ForInvalidToken()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var invalidToken = "invalid.jwt.token";

        // Act
        var userId = service.GetUserIdFromToken(invalidToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);

        // Act
        var refreshToken = service.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);

        // Act
        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateAndExtractUserId_ReturnsValidAndUserId_ForValidToken()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Act
        var (isValid, extractedUserId) = service.ValidateAndExtractUserId(token);

        // Assert
        isValid.Should().BeTrue();
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void ValidateAndExtractUserId_ReturnsInvalidAndNull_ForInvalidToken()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var invalidToken = "invalid.jwt.token";

        // Act
        var (isValid, userId) = service.ValidateAndExtractUserId(invalidToken);

        // Assert
        isValid.Should().BeFalse();
        userId.Should().BeNull();
    }
}

/// <summary>
/// Tests for JwtService using RS256 asymmetric signing (recommended for production)
/// </summary>
public class JwtServiceRs256Tests
{
    private readonly Mock<ILogger<JwtService>> _mockLogger;
    private readonly IConfiguration _configuration;

    // Test RSA key pair for RS256 signing in unit tests only
    // Generated for testing purposes - DO NOT use in production
    private const string TestRsaPrivateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDbnlQ7tTRx2q6Y
e+jssAP9//VhK0b9YCpQrTkYt5aUAhzjt6c+V+ybDkLBK1ZQilnC1vUiu4OBJCxW
agfoMigGOnz7rczVFSALsXqR5EX+J7orDJGmjwAbn9EmvhGqeAh3w9/0Qd4gfeEA
ys7mWpr803V0aBMtcy2TVghyTidrRU0sJ5hMOmzf5hcQ0E/oIyTbcLLi9M7ZZiTR
VxuZ+BVcmXPCjYNXYNjFl8vaGiNzQJI/446YfS6OdgSYb+wAyAyAKBnZwyf6xWtJ
UIr/KluMm79zl+mUxOoJihQVge4mWDd5Ru80eQ/VfSX2bKcwMS5kmMeww/zAU+of
+UcsU5UVAgMBAAECggEATI6BQ30TxpqaIVqUZCmpgpn+sjwxV3L13UC9NhINYhPo
eTMUkEV7G8QZXhga0yGfT626LzzZhyOSdx8oGXeefylVVzCLRj5CeQEJvqCqC4JS
wd30SfDwczC/and4VgnYvdMglxd89Kucyzdnb2JnQ7n86DK9eKr9WK51bE81K42M
HLbg9jSEocCJFII26FAiLwihfURZP5E1Z4sHU65pFV3EUvA9atlsxzrGM3HErRZv
2DUjMrGpWK0a+pASNfktuuRkC6jVDpW2h1qf11qvJvaoPMgwGK4E2GSP0hEXOrbK
rQuqIihygLNxRUzgZNIF/hjAqFBN+OUkiebRuyXDwwKBgQDtztqTcKx+3rjm4ns5
vCe9tzNTRI7tTuGDP5TW+K/GpV6OanG1uNaVZjwTi/5R3PoIVi/V/zACeOyChLZn
/8xcyiZC0jE7KpXpZCYM1rKAZ+paYbJ3be2GdeZBL4InC7CW9C/kXUu3OoOf9cZc
YLCx9el8ksg2wTAqHyjdYcHLswKBgQDsa0H2awsxzTx2pcgrkC1ra5ar6fiY4O0A
XHn51c0Krss5J6vU74UYjcKyERJPLqTV5D68C+XwbUAXmTkrAfK1VHAPXC+JWMCK
ptUgIwcMT/RohZ5TG5hbSRVz8geKPKWmhEpj95VIaVWC7IFzh6beIjKLBFWESLzJ
f+CSBSiYFwKBgB3oL9DvEKJ7/CD9RqYCJbVUPt4v9xGdI/tPmbZXXDPNRFEAzgAe
mM39J30F1BwTgFZgEHAHQdBtyMC5U/9MSjU5LwqkSJC6UFQjxi1DKvu/Fdf8BWfD
qWWJmkWEZgfnDnRNjWBY41bNwxPw4ttnRZF77bs+8nMAZMBHXupIiwjFAoGBAJag
o683djNturcxWr5+pqGJM78mW9Azhmyzfrdxw6ipwysQHoeVb2w8ba579/lhE35/
ZIT047RyNuKSKf0/yX5EZP00U8kjNdFhB+roxkXO7z5k24HB1CldAAEWVD179GKK
aMcWaBNxoRzASJ3t8KAYk7FEuqOEoFuVUORXywxTAoGACp6vXaVX7YWV3WjoB+5C
TkZvt4LOCVkOX9T0I5rzFTv4qpcjDwhcRIbS3GiP0ZnXQjXH3OF/uFa5vPyohPJl
n6bjFgPio+QC4aaRR9K/bYThEFOsz+9uMNofv+6TFS8DLKSKRWrYpnhOjk9Pj5Ga
qezmNuL8yfgbWOehKPnIVjw=
-----END PRIVATE KEY-----";

    private const string TestRsaPublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA255UO7U0cdqumHvo7LAD
/f/1YStG/WAqUK05GLeWlAIc47enPlfsmw5CwStWUIpZwtb1IruDgSQsVmoH6DIo
Bjp8+63M1RUgC7F6keRF/ie6KwyRpo8AG5/RJr4RqngId8Pf9EHeIH3hAMrO5lqa
/NN1dGgTLXMtk1YIck4na0VNLCeYTDps3+YXENBP6CMk23Cy4vTO2WYk0VcbmfgV
XJlzwo2DV2DYxZfL2hojc0CSP+OOmH0ujnYEmG/sAMgMgCgZ2cMn+sVrSVCK/ypb
jJu/c5fplMTqCYoUFYHuJlg3eUbvNHkP1X0l9mynMDEuZJjHsMP8wFPqH/lHLFOV
FQIDAQAB
-----END PUBLIC KEY-----";

    public JwtServiceRs256Tests()
    {
        _mockLogger = new Mock<ILogger<JwtService>>();

        // Create test configuration with RSA key pair for RS256 testing
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "MystiraAPI",
                ["JwtSettings:Audience"] = "MystiraPWA",
                ["JwtSettings:RsaPrivateKey"] = TestRsaPrivateKey
            })
            .Build();
    }

    [Fact]
    public void Constructor_InitializesWithRs256_WhenRsaPrivateKeyProvided()
    {
        // Arrange & Act
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";

        // Act - Generate a token
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Assert - Token should be generated successfully
        token.Should().NotBeNullOrEmpty();

        // Verify the token uses RS256 algorithm
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.Header.Alg.Should().Be("RS256");
    }

    [Fact]
    public void GenerateAccessToken_CreatesValidRs256Token_WithRequiredClaims()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";

        // Act
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Decode and verify token claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("MystiraAPI");
        jwtToken.Audiences.Should().Contain("MystiraPWA");
        jwtToken.Header.Alg.Should().Be("RS256");

        // Verify standard claims are present
        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == "sub" && c.Value == userId);
        claims.Should().Contain(c => c.Type == "email" && c.Value == email);
        claims.Should().Contain(c => c.Type == "name" && c.Value == displayName);
    }

    [Fact]
    public void ValidateToken_ReturnsTrue_ForValidRs256Token()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Act
        var isValid = service.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ReturnsFalse_ForTokenSignedWithDifferentKey()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);

        // Create a token with a different RSA key
        using (var differentRsa = RSA.Create(2048))
        {
            var differentKey = new RsaSecurityKey(differentRsa);
            var credentials = new SigningCredentials(differentKey, SecurityAlgorithms.RsaSha256);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user"),
                    new Claim("sub", "test-user")
                }),
                Expires = DateTime.UtcNow.AddHours(6),
                Issuer = "MystiraAPI",
                Audience = "MystiraPWA",
                SigningCredentials = credentials
            };
            var maliciousToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

            // Act
            var isValid = service.ValidateToken(maliciousToken);

            // Assert
            isValid.Should().BeFalse();
        }

        // Act
        var isValid = service.ValidateToken(maliciousToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetUserIdFromToken_ExtractsUserId_FromValidRs256Token()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-rs256";
        var email = "test@example.com";
        var displayName = "Test User";
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Act
        var extractedUserId = service.GetUserIdFromToken(token);

        // Assert
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void ValidateAndExtractUserId_ReturnsValidAndUserId_ForValidRs256Token()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-rs256";
        var email = "test@example.com";
        var displayName = "Test User";
        var token = service.GenerateAccessToken(userId, email, displayName);

        // Act
        var (isValid, extractedUserId) = service.ValidateAndExtractUserId(token);

        // Assert
        isValid.Should().BeTrue();
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void Constructor_ThrowsException_WhenInvalidRsaPrivateKeyProvided()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "MystiraAPI",
                ["JwtSettings:Audience"] = "MystiraPWA",
                ["JwtSettings:RsaPrivateKey"] = "invalid-pem-key"
            })
            .Build();

        // Act & Assert
        var act = () => new JwtService(invalidConfig, _mockLogger.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to load RSA private key*");
    }

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaim_WithRs256()
    {
        // Arrange
        var service = new JwtService(_configuration, _mockLogger.Object);
        var userId = "test-user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var role = "Admin";

        // Act
        var token = service.GenerateAccessToken(userId, email, displayName, role);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Header.Alg.Should().Be("RS256");

        var claims = jwtToken.Claims.ToList();
        var roleClaims = claims.Where(c => c.Type == "role").ToList();
        roleClaims.Should().NotBeEmpty();
        roleClaims.Should().Contain(c => c.Value == role);
    }
}
