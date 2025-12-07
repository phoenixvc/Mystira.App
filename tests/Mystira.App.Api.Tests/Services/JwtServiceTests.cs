using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        var afterGeneration = DateTime.UtcNow;

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
