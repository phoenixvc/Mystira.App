using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Services;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class EntraExternalIdAuthServiceTests : IDisposable
{
    private readonly Mock<ILogger<EntraExternalIdAuthService>> _mockLogger;
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly EntraExternalIdAuthService _service;

    private const string TestAuthority = "https://test.ciamlogin.com/tenant-id/v2.0";
    private const string TestClientId = "test-client-id";
    private const string TestRedirectUri = "http://localhost:5173/authentication/login-callback";

    public EntraExternalIdAuthServiceTests()
    {
        _mockLogger = new Mock<ILogger<EntraExternalIdAuthService>>();
        _mockApiClient = new Mock<IApiClient>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockConfiguration = new Mock<IConfiguration>();

        SetupDefaultConfiguration();

        _service = new EntraExternalIdAuthService(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockJsRuntime.Object,
            _mockConfiguration.Object);
    }

    private void SetupDefaultConfiguration()
    {
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:Authority"]).Returns(TestAuthority);
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:ClientId"]).Returns(TestClientId);
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:RedirectUri"]).Returns(TestRedirectUri);
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:PostLogoutRedirectUri"]).Returns("http://localhost:5173");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntraExternalIdAuthService(
            null!,
            _mockApiClient.Object,
            _mockJsRuntime.Object,
            _mockConfiguration.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullApiClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntraExternalIdAuthService(
            _mockLogger.Object,
            null!,
            _mockJsRuntime.Object,
            _mockConfiguration.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_WithNullJSRuntime_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntraExternalIdAuthService(
            _mockLogger.Object,
            _mockApiClient.Object,
            null!,
            _mockConfiguration.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("jsRuntime");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntraExternalIdAuthService(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockJsRuntime.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    #endregion

    #region IsAuthenticatedAsync Tests

    [Fact]
    public async Task IsAuthenticatedAsync_WithNoStoredData_ReturnsFalse()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.IsAuthenticatedAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WithStoredToken_ReturnsTrue()
    {
        // Arrange
        var testToken = "test-token";
        var testAccount = new Account { Email = "test@example.com", DisplayName = "Test User" };
        var accountJson = JsonSerializer.Serialize(testAccount);

        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync(testToken);
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_account" }))
            .ReturnsAsync(accountJson);

        // Act
        var result = await _service.IsAuthenticatedAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WithJSException_ReturnsFalse()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JavaScript error"));

        // Act
        var result = await _service.IsAuthenticatedAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WithJsonException_ReturnsFalse()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync("test-token");
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_account" }))
            .ReturnsAsync("invalid-json{");

        // Act
        var result = await _service.IsAuthenticatedAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetCurrentAccountAsync Tests

    [Fact]
    public async Task GetCurrentAccountAsync_WithNoStoredData_ReturnsNull()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetCurrentAccountAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentAccountAsync_WithStoredAccount_ReturnsAccount()
    {
        // Arrange
        var testAccount = new Account { Email = "test@example.com", DisplayName = "Test User" };
        var accountJson = JsonSerializer.Serialize(testAccount);

        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_account" }))
            .ReturnsAsync(accountJson);

        // Act
        var result = await _service.GetCurrentAccountAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.DisplayName.Should().Be("Test User");
    }

    #endregion

    #region GetTokenAsync Tests

    [Fact]
    public async Task GetTokenAsync_WithNoStoredToken_ReturnsNull()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetTokenAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenAsync_WithStoredToken_ReturnsToken()
    {
        // Arrange
        var testToken = "test-access-token";
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync(testToken);

        // Act
        var result = await _service.GetTokenAsync();

        // Assert
        result.Should().Be(testToken);
    }

    #endregion

    #region LoginWithEntraAsync Tests

    [Fact]
    public async Task LoginWithEntraAsync_WithValidConfiguration_RedirectsToAuthUrl()
    {
        // Arrange
        string? capturedUrl = null;
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("sessionStorage.setItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("window.location.href", It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) => capturedUrl = args[0] as string)
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.LoginWithEntraAsync();

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().StartWith($"{TestAuthority}/oauth2/authorize?");
        capturedUrl.Should().Contain($"client_id={Uri.EscapeDataString(TestClientId)}");
        capturedUrl.Should().Contain($"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}");
        capturedUrl.Should().Contain("response_type=id_token%20token");
        capturedUrl.Should().Contain("response_mode=fragment");
        capturedUrl.Should().Contain("scope=openid%20profile%20email%20offline_access");
        capturedUrl.Should().Contain("state=");
        capturedUrl.Should().Contain("nonce=");
    }

    [Fact]
    public async Task LoginWithEntraAsync_WithMissingAuthority_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:Authority"]).Returns((string?)null);

        // Act
        var act = async () => await _service.LoginWithEntraAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task LoginWithEntraAsync_WithMissingClientId_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:ClientId"]).Returns((string?)null);

        // Act
        var act = async () => await _service.LoginWithEntraAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task LoginWithEntraAsync_StoresStateAndNonceInSessionStorage()
    {
        // Arrange
        var storedValues = new Dictionary<string, string>();
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("sessionStorage.setItem", It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) =>
            {
                if (args.Length == 2)
                {
                    storedValues[args[0] as string ?? ""] = args[1] as string ?? "";
                }
            })
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("window.location.href", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.LoginWithEntraAsync();

        // Assert
        storedValues.Should().ContainKey("entra_auth_state");
        storedValues.Should().ContainKey("entra_auth_nonce");
        storedValues["entra_auth_state"].Should().NotBeNullOrEmpty();
        storedValues["entra_auth_nonce"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginWithEntraAsync_WithJSException_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("sessionStorage.setItem", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JavaScript error"));

        // Act
        var act = async () => await _service.LoginWithEntraAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithInnerException<JSException>();
    }

    #endregion

    #region HandleLoginCallbackAsync Tests

    [Fact]
    public async Task HandleLoginCallbackAsync_WithValidTokens_ReturnsTrue()
    {
        // Arrange
        var testAccessToken = "test-access-token";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123");
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=test-state";

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", new object[] { "window.location.hash" }))
            .ReturnsAsync(fragment);
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("sessionStorage.getItem", new object[] { "entra_auth_state" }))
            .ReturnsAsync("test-state");
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("localStorage.setItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("sessionStorage.removeItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _service.HandleLoginCallbackAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleLoginCallbackAsync_WithNoFragment_ReturnsFalse()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", new object[] { "window.location.hash" }))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.HandleLoginCallbackAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleLoginCallbackAsync_WithMissingAccessToken_ReturnsFalse()
    {
        // Arrange
        var fragment = "#id_token=test-id-token&state=test-state";
        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", new object[] { "window.location.hash" }))
            .ReturnsAsync(fragment);

        // Act
        var result = await _service.HandleLoginCallbackAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleLoginCallbackAsync_WithStateMismatch_ReturnsFalse()
    {
        // Arrange
        var testAccessToken = "test-access-token";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123");
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=wrong-state";

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", new object[] { "window.location.hash" }))
            .ReturnsAsync(fragment);
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("sessionStorage.getItem", new object[] { "entra_auth_state" }))
            .ReturnsAsync("correct-state");

        // Act
        var result = await _service.HandleLoginCallbackAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleLoginCallbackAsync_StoresTokensInLocalStorage()
    {
        // Arrange
        var testAccessToken = "test-access-token";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123");
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=test-state";
        var storedValues = new Dictionary<string, string>();

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", new object[] { "window.location.hash" }))
            .ReturnsAsync(fragment);
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("sessionStorage.getItem", new object[] { "entra_auth_state" }))
            .ReturnsAsync("test-state");
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("localStorage.setItem", It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) =>
            {
                if (args.Length == 2)
                {
                    storedValues[args[0] as string ?? ""] = args[1] as string ?? "";
                }
            })
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("sessionStorage.removeItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.HandleLoginCallbackAsync();

        // Assert
        storedValues.Should().ContainKey("mystira_entra_token");
        storedValues.Should().ContainKey("mystira_entra_id_token");
        storedValues.Should().ContainKey("mystira_entra_account");
        storedValues["mystira_entra_token"].Should().Be(testAccessToken);
        storedValues["mystira_entra_id_token"].Should().Be(testIdToken);
    }

    [Fact]
    public async Task HandleLoginCallbackAsync_RaisesAuthenticationStateChangedEvent()
    {
        // Arrange
        var testAccessToken = "test-access-token";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123");
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=test-state";
        var eventRaised = false;
        var authenticatedState = false;

        _service.AuthenticationStateChanged += (sender, isAuthenticated) =>
        {
            eventRaised = true;
            authenticatedState = isAuthenticated;
        };

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", new object[] { "window.location.hash" }))
            .ReturnsAsync(fragment);
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("sessionStorage.getItem", new object[] { "entra_auth_state" }))
            .ReturnsAsync("test-state");
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("localStorage.setItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("sessionStorage.removeItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.HandleLoginCallbackAsync();

        // Assert
        eventRaised.Should().BeTrue();
        authenticatedState.Should().BeTrue();
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ClearsLocalStorage()
    {
        // Arrange
        var clearedKeys = new List<string>();
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("localStorage.removeItem", It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) =>
            {
                if (args.Length == 1)
                {
                    clearedKeys.Add(args[0] as string ?? "");
                }
            })
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("window.location.href", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.LogoutAsync();

        // Assert
        clearedKeys.Should().Contain("mystira_entra_token");
        clearedKeys.Should().Contain("mystira_entra_id_token");
        clearedKeys.Should().Contain("mystira_entra_account");
    }

    [Fact]
    public async Task LogoutAsync_RaisesAuthenticationStateChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        var authenticatedState = true;

        _service.AuthenticationStateChanged += (sender, isAuthenticated) =>
        {
            eventRaised = true;
            authenticatedState = isAuthenticated;
        };

        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("localStorage.removeItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("window.location.href", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.LogoutAsync();

        // Assert
        eventRaised.Should().BeTrue();
        authenticatedState.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_RedirectsToLogoutEndpoint()
    {
        // Arrange
        string? capturedUrl = null;
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("localStorage.removeItem", It.IsAny<object[]>()))
            .Returns(ValueTask.CompletedTask);
        _mockJsRuntime.Setup(js => js.InvokeVoidAsync("window.location.href", It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) => capturedUrl = args[0] as string)
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.LogoutAsync();

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().StartWith($"{TestAuthority}/oauth2/logout?");
        capturedUrl.Should().Contain("post_logout_redirect_uri=");
    }

    #endregion

    #region GetTokenExpiryTime Tests

    [Fact]
    public void GetTokenExpiryTime_WithNoToken_ReturnsNull()
    {
        // Act
        var result = _service.GetTokenExpiryTime();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenExpiryTime_WithValidToken_ReturnsExpiryTime()
    {
        // Arrange
        var expiryTimestamp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var testToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", expiryTimestamp);
        
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync(testToken);

        // First load the token
        await _service.GetTokenAsync();

        // Act
        var result = _service.GetTokenExpiryTime();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().BeCloseTo(DateTimeOffset.FromUnixTimeSeconds(expiryTimestamp).UtcDateTime, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region EnsureTokenValidAsync Tests

    [Fact]
    public async Task EnsureTokenValidAsync_WithNoToken_ReturnsFalse()
    {
        // Act
        var result = await _service.EnsureTokenValidAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureTokenValidAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var expiryTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var testToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", expiryTimestamp);
        
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync(testToken);

        await _service.GetTokenAsync();

        // Act
        var result = await _service.EnsureTokenValidAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureTokenValidAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var expiryTimestamp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var testToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", expiryTimestamp);
        
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync(testToken);

        await _service.GetTokenAsync();

        // Act
        var result = await _service.EnsureTokenValidAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureTokenValidAsync_WithSoonToExpireToken_RaisesWarningEvent()
    {
        // Arrange
        var expiryTimestamp = DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds();
        var testToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", expiryTimestamp);
        var eventRaised = false;

        _service.TokenExpiryWarning += (sender, args) =>
        {
            eventRaised = true;
        };

        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", new object[] { "mystira_entra_token" }))
            .ReturnsAsync(testToken);

        await _service.GetTokenAsync();

        // Act
        await _service.EnsureTokenValidAsync(expiryBufferMinutes: 5);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static string CreateTestIdToken(string email, string name, string sub, long? exp = null)
    {
        var header = new { alg = "RS256", typ = "JWT" };
        var payload = new
        {
            email,
            name,
            sub,
            exp = exp ?? DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            aud = TestClientId,
            iss = TestAuthority
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Fake signature (not validated in tests)
        var signature = "fake-signature";

        return $"{headerBase64}.{payloadBase64}.{signature}";
    }

    #endregion

    public void Dispose()
    {
        // Cleanup if needed
    }
}
