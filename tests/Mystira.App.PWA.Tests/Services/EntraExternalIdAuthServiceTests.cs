using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
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
    private readonly TestNavigationManager _navigationManager;
    private readonly HttpClient _httpClient;
    private readonly EntraExternalIdAuthService _service;

    private const string TestAuthority = "https://test.ciamlogin.com/tenant-id/v2.0";
    private const string TestClientId = "test-client-id";
    private const string TestRedirectUri = "http://localhost:7000/authentication/login-callback";

    public EntraExternalIdAuthServiceTests()
    {
        _mockLogger = new Mock<ILogger<EntraExternalIdAuthService>>();
        _mockApiClient = new Mock<IApiClient>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockConfiguration = new Mock<IConfiguration>();
        _navigationManager = new TestNavigationManager();
        _httpClient = new HttpClient();

        SetupDefaultConfiguration();

        _service = new EntraExternalIdAuthService(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockJsRuntime.Object,
            _mockConfiguration.Object,
            _navigationManager,
            _httpClient);
    }

    private class TestNavigationManager : NavigationManager
    {
        public string? NavigatedUrl { get; private set; }

        public TestNavigationManager()
        {
            Initialize("http://localhost:7000/", "http://localhost:7000/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedUrl = uri;
        }
    }

    private void SetupDefaultConfiguration()
    {
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:Authority"]).Returns(TestAuthority);
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:ClientId"]).Returns(TestClientId);
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:RedirectUri"]).Returns(TestRedirectUri);
        _mockConfiguration.Setup(c => c["MicrosoftEntraExternalId:PostLogoutRedirectUri"]).Returns("http://localhost:7000");
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
            _mockConfiguration.Object,
            _navigationManager,
            _httpClient);

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
            _mockConfiguration.Object,
            _navigationManager,
            _httpClient);

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
            _mockConfiguration.Object,
            _navigationManager,
            _httpClient);

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
            null!,
            _navigationManager,
            _httpClient);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullNavigationManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntraExternalIdAuthService(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockJsRuntime.Object,
            _mockConfiguration.Object,
            null!,
            _httpClient);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("navigationManager");
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntraExternalIdAuthService(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockJsRuntime.Object,
            _mockConfiguration.Object,
            _navigationManager,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
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
        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        await _service.LoginWithEntraAsync();

        // Assert
        var capturedUrl = _navigationManager.NavigatedUrl;
        capturedUrl.Should().NotBeNull();
        // The authority in tests is "https://test.ciamlogin.com/tenant-id/v2.0"
        // Our service should strip "/v2.0" and append "/oauth2/v2.0/authorize"
        capturedUrl.Should().StartWith("https://test.ciamlogin.com/tenant-id/oauth2/v2.0/authorize?");
        capturedUrl.Should().Contain($"client_id={Uri.EscapeDataString(TestClientId)}");
        capturedUrl.Should().Contain($"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}");
        capturedUrl.Should().Contain("response_type=code");
        capturedUrl.Should().Contain("response_mode=fragment");
        capturedUrl.Should().Contain("scope=openid%20profile%20email%20offline_access");
        capturedUrl.Should().Contain("state=");
        capturedUrl.Should().Contain("nonce=");
        capturedUrl.Should().Contain("code_challenge=");
        capturedUrl.Should().Contain("code_challenge_method=S256");
    }

    [Fact]
    public async Task LoginWithEntraAsync_WithDomainHint_RedirectsWithDomainHintOnly()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        await _service.LoginWithEntraAsync("Google");

        // Assert
        var capturedUrl = _navigationManager.NavigatedUrl;
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("domain_hint=Google");
        capturedUrl.Should().NotContain("direct_signin=");
        capturedUrl.Should().NotContain("prompt=");
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
        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((identifier, args) =>
            {
                if (identifier == "sessionStorage.setItem" && args.Length == 2)
                {
                    storedValues[args[0] as string ?? ""] = args[1] as string ?? "";
                }
            })
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        await _service.LoginWithEntraAsync();

        // Assert
        storedValues.Should().ContainKey("entra_auth_state");
        storedValues.Should().ContainKey("entra_auth_nonce");
        storedValues.Should().ContainKey("entra_auth_code_verifier");
        storedValues["entra_auth_state"].Should().NotBeNullOrEmpty();
        storedValues["entra_auth_nonce"].Should().NotBeNullOrEmpty();
        storedValues["entra_auth_code_verifier"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginWithEntraAsync_WithJSException_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JavaScript error"));

        // Act
        var act = async () => await _service.LoginWithEntraAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region HandleLoginCallbackAsync Tests

    [Fact]
    public async Task HandleLoginCallbackAsync_WithValidTokens_ReturnsTrue()
    {
        // Arrange
        var testAccessToken = "test-access-token";
        var testNonce = "test-nonce";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", nonce: testNonce);
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=test-state";

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string identifier, object[] args) =>
            {
                if (identifier == "eval") return fragment;
                if (identifier == "sessionStorage.getItem")
                {
                    if (args.Length > 0 && (string)args[0] == "entra_auth_state") return "test-state";
                    if (args.Length > 0 && (string)args[0] == "entra_auth_nonce") return testNonce;
                }
                return null!;
            });

        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        var result = await _service.HandleLoginCallbackAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleLoginCallbackAsync_WithNoFragment_ReturnsFalse()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync("");

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
        _mockJsRuntime.Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
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

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(fragment);
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string identifier, object[] args) =>
                identifier == "sessionStorage.getItem" && (string)args[0] == "entra_auth_state" ? "correct-state" : null);

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
        var testNonce = "test-nonce";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", nonce: testNonce);
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=test-state";
        var storedValues = new Dictionary<string, string>();

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string identifier, object[] args) =>
            {
                if (identifier == "eval") return fragment;
                if (identifier == "sessionStorage.getItem")
                {
                    if (args.Length > 0 && (string)args[0] == "entra_auth_state") return "test-state";
                    if (args.Length > 0 && (string)args[0] == "entra_auth_nonce") return testNonce;
                }
                return null!;
            });

        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((identifier, args) =>
            {
                if ((identifier == "localStorage.setItem" || identifier == "sessionStorage.setItem") && args.Length == 2)
                {
                    storedValues[args[0] as string ?? ""] = args[1] as string ?? "";
                }
            })
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

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
        var testNonce = "test-nonce";
        var testIdToken = CreateTestIdToken("test@example.com", "Test User", "sub-123", nonce: testNonce);
        var fragment = $"#access_token={testAccessToken}&id_token={testIdToken}&state=test-state";
        var eventRaised = false;
        var authenticatedState = false;

        _service.AuthenticationStateChanged += (sender, isAuthenticated) =>
        {
            eventRaised = true;
            authenticatedState = isAuthenticated;
        };

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string identifier, object[] args) =>
            {
                if (identifier == "eval") return fragment;
                if (identifier == "sessionStorage.getItem")
                {
                    if (args.Length > 0 && (string)args[0] == "entra_auth_state") return "test-state";
                    if (args.Length > 0 && (string)args[0] == "entra_auth_nonce") return testNonce;
                }
                return null!;
            });

        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

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
        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((identifier, args) =>
            {
                if (identifier == "localStorage.removeItem" && args.Length == 1)
                {
                    clearedKeys.Add(args[0] as string ?? "");
                }
            })
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

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

        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        await _service.LogoutAsync();

        // Assert
        eventRaised.Should().BeTrue();
        authenticatedState.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_RedirectsToLogoutEndpointWithIdTokenHint()
    {
        // Arrange
        var testIdToken = "test-id-token";
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "mystira_entra_id_token")))
            .ReturnsAsync(testIdToken);

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "window.location.origin")))
            .ReturnsAsync("http://localhost:7000");

        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        await _service.LogoutAsync();

        // Assert
        var capturedUrl = _navigationManager.NavigatedUrl;
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().StartWith("https://test.ciamlogin.com/tenant-id/oauth2/v2.0/logout?");
        capturedUrl.Should().Contain("post_logout_redirect_uri=");
        capturedUrl.Should().Contain($"id_token_hint={testIdToken}");
    }

    [Fact]
    public async Task LogoutAsync_RedirectsToLogoutEndpointWithoutIdTokenHintWhenMissing()
    {
        // Arrange
        _mockJsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "mystira_entra_id_token")))
            .ReturnsAsync((string?)null);

        _mockJsRuntime.Setup(js => js.InvokeAsync<string>("eval", It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "window.location.origin")))
            .ReturnsAsync("http://localhost:7000");

        _mockJsRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        await _service.LogoutAsync();

        // Assert
        var capturedUrl = _navigationManager.NavigatedUrl;
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().StartWith("https://test.ciamlogin.com/tenant-id/oauth2/v2.0/logout?");
        capturedUrl.Should().Contain("post_logout_redirect_uri=");
        capturedUrl.Should().NotContain("id_token_hint=");
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

    private static string CreateTestIdToken(string email, string name, string sub, long? exp = null, string? nonce = null)
    {
        var header = new { alg = "RS256", typ = "JWT" };
        var payload = new
        {
            email,
            name,
            sub,
            nonce,
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
