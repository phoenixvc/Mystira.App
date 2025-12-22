using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Authentication service for Microsoft Entra External ID (CIAM)
/// Supports Google social login and email+password authentication
/// Uses Authorization Code Flow with PKCE (Proof Key for Code Exchange)
/// </summary>
public class EntraExternalIdAuthService : IAuthService
{
    private readonly ILogger<EntraExternalIdAuthService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly NavigationManager _navigationManager;
    private readonly HttpClient _httpClient;

    private const string TokenStorageKey = "mystira_entra_token";
    private const string AccountStorageKey = "mystira_entra_account";
    private const string IdTokenStorageKey = "mystira_entra_id_token";
    private const string AuthStateKey = "entra_auth_state";
    private const string AuthNonceKey = "entra_auth_nonce";
    private const string AuthCodeVerifierKey = "entra_auth_code_verifier";

    private static readonly string[] DefaultScopes = { "openid", "profile", "email", "offline_access" };

    private bool _isAuthenticated;
    private string? _currentToken;
    private Account? _currentAccount;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    public event EventHandler<bool>? AuthenticationStateChanged;
    public event EventHandler? TokenExpiryWarning;

    public EntraExternalIdAuthService(
        ILogger<EntraExternalIdAuthService> logger,
        IApiClient apiClient,
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        NavigationManager navigationManager,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    #region IAuthService Implementation

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            if (_isAuthenticated && !string.IsNullOrEmpty(_currentToken) && _currentAccount != null)
            {
                return true;
            }

            await LoadStoredAuthDataAsync();
            _isAuthenticated = !string.IsNullOrEmpty(_currentToken) && _currentAccount != null;

            return _isAuthenticated;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error checking authentication status");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error checking authentication status");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Authentication status check was canceled");
            return false;
        }
    }

    public async Task<Account?> GetCurrentAccountAsync()
    {
        try
        {
            if (_currentAccount == null)
            {
                await LoadStoredAuthDataAsync();
            }

            return _currentAccount;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error getting current account");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error getting current account");
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get current account operation was canceled");
            return null;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentToken))
            {
                await LoadStoredAuthDataAsync();
            }

            return _currentToken;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error getting token");
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get token operation was canceled");
            return null;
        }
    }

    public async Task<string?> GetCurrentTokenAsync()
    {
        return await GetTokenAsync();
    }

    public void SetRememberMe(bool rememberMe)
    {
        // Not applicable for Entra External ID - handled by the identity provider
        _logger.LogDebug("SetRememberMe called with {RememberMe} - handled by Entra External ID", rememberMe);
    }

    public Task<bool> GetRememberMeAsync()
    {
        // Not applicable for Entra External ID
        return Task.FromResult(false);
    }

    public Task<bool> LoginAsync(string email, string password)
    {
        _logger.LogWarning("LoginAsync called but not supported with Entra External ID. Use LoginWithEntraAsync instead.");
        return Task.FromResult(false);
    }

    public Task<(bool Success, string Message)> RequestPasswordlessSignupAsync(string email, string displayName)
    {
        _logger.LogWarning("RequestPasswordlessSignupAsync not supported with Entra External ID");
        return Task.FromResult((false, "Use Entra External ID login instead"));
    }

    public Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSignupAsync(string email, string code)
    {
        _logger.LogWarning("VerifyPasswordlessSignupAsync not supported with Entra External ID");
        return Task.FromResult<(bool, string, Account?)>((false, "Use Entra External ID login instead", null));
    }

    public Task<(bool Success, string Message)> RequestPasswordlessSigninAsync(string email)
    {
        _logger.LogWarning("RequestPasswordlessSigninAsync not supported with Entra External ID");
        return Task.FromResult((false, "Use Entra External ID login instead"));
    }

    public Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSigninAsync(string email, string code)
    {
        _logger.LogWarning("VerifyPasswordlessSigninAsync not supported with Entra External ID");
        return Task.FromResult<(bool, string, Account?)>((false, "Use Entra External ID login instead", null));
    }

    public Task<(bool Success, string Message, string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken)
    {
        _logger.LogWarning("RefreshTokenAsync not implemented for Entra External ID - tokens are managed by the identity provider");
        return Task.FromResult<(bool, string, string?, string?)>((false, "Token refresh handled by Entra External ID", null, null));
    }

    public DateTime? GetTokenExpiryTime()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentToken))
            {
                return null;
            }

            var claims = DecodeJwtPayload(_currentToken);
            if (claims != null && claims.TryGetValue("exp", out var expClaim))
            {
                var exp = expClaim.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid JWT format when getting token expiry time");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error decoding token expiry time");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when getting token expiry time");
        }

        return null;
    }

    public async Task<bool> EnsureTokenValidAsync(int expiryBufferMinutes = 5)
    {
        try
        {
            var expiryTime = GetTokenExpiryTime();
            if (expiryTime == null)
            {
                _logger.LogWarning("Cannot determine token expiry time");
                return false;
            }

            var timeUntilExpiry = expiryTime.Value - DateTime.UtcNow;

            if (timeUntilExpiry.TotalMinutes <= expiryBufferMinutes)
            {
                _logger.LogWarning("Token will expire in {Minutes} minutes", timeUntilExpiry.TotalMinutes);
                TokenExpiryWarning?.Invoke(this, EventArgs.Empty);

                // For Entra External ID, user needs to re-authenticate
                // We can't silently refresh tokens in the implicit flow
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Token validation check was canceled");
            return false;
        }
    }

    #endregion

    #region Entra External ID Specific Methods

    /// <summary>
    /// Initiates login flow with Microsoft Entra External ID
    /// Redirects to Entra login page which supports Google social login
    /// </summary>
    /// <param name="domainHint">Optional domain hint to skip the Entra signin page (e.g., "google.com" for Google)</param>
    public async Task LoginWithEntraAsync(string? domainHint = null)
    {
        try
        {
            _logger.LogInformation("Initiating Entra External ID login with domain hint: {DomainHint}", domainHint ?? "none");

            var (authority, clientId, redirectUri) = GetEntraConfiguration();
            ValidateEntraConfiguration(authority, clientId);

            var (state, nonce, codeVerifier) = await GenerateAndStoreSecurityTokensAsync();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var authUrl = BuildAuthorizationUrl(authority, clientId, redirectUri, state, nonce, codeChallenge, domainHint);

            _logger.LogInformation("Redirecting to Entra External ID: {AuthUrl}", authUrl);
            _navigationManager.NavigateTo(authUrl);
        }
        catch (InvalidOperationException)
        {
            throw; // Rethrow configuration errors
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error initiating Entra External ID login");
            throw new InvalidOperationException("Failed to initiate login due to JavaScript error", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Login initiation was canceled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initiating Entra External ID login");
            throw new InvalidOperationException("Failed to initiate login due to unexpected error", ex);
        }
    }

    /// <summary>
    /// Handles the callback from Entra External ID after authentication
    /// </summary>
    public async Task<bool> HandleLoginCallbackAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            _logger.LogInformation("Handling Entra External ID login callback");

            var fragment = await GetUrlFragmentAsync();
            if (string.IsNullOrEmpty(fragment))
            {
                _logger.LogWarning("No fragment found in callback URL");
                return false;
            }

            var parameters = ParseFragment(fragment);

            if (!await ValidateStateAsync(parameters))
            {
                _logger.LogWarning("State validation failed in callback");
                return false;
            }

            string accessToken;
            string idToken;

            if (parameters.TryGetValue("code", out var code))
            {
                _logger.LogInformation("Found authorization code, exchanging for tokens");
                var tokens = await ExchangeCodeForTokensAsync(code);
                if (tokens == null)
                {
                    _logger.LogWarning("Failed to exchange authorization code for tokens");
                    return false;
                }
                accessToken = tokens.Value.AccessToken;
                idToken = tokens.Value.IdToken;
            }
            else if (TryExtractTokens(parameters, out accessToken, out idToken))
            {
                _logger.LogInformation("Found tokens in fragment (Implicit Flow fallback)");
            }
            else
            {
                _logger.LogWarning("Authorization code or tokens missing from callback");
                return false;
            }

            await StoreTokensAsync(accessToken, idToken);
            _currentToken = accessToken;

            var account = ExtractAccountFromIdToken(idToken);
            if (account != null)
            {
                await SetStoredAccountAsync(account);
                _isAuthenticated = true;

                _logger.LogInformation("Entra External ID login successful for: {Email}", account.Email);
                AuthenticationStateChanged?.Invoke(this, true);

                await ClearAuthStateAsync();

                return true;
            }

            return false;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error handling Entra External ID login callback");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error handling Entra External ID login callback");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Login callback handling was canceled");
            return false;
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user from Entra External ID");

            var authority = _configuration["MicrosoftEntraExternalId:Authority"];
            var postLogoutRedirectUri = _configuration["MicrosoftEntraExternalId:PostLogoutRedirectUri"]
                ?? await GetCurrentOriginAsync();

            // Get id_token before clearing storage to use as hint for logout
            var idTokenHint = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", IdTokenStorageKey);

            await ClearLocalStorageAsync();
            ClearAuthenticationState();

            _logger.LogInformation("Local logout successful");
            AuthenticationStateChanged?.Invoke(this, false);

            // Redirect to Entra logout endpoint
            if (!string.IsNullOrEmpty(authority))
            {
                var logoutUrl = BuildLogoutUrl(authority, postLogoutRedirectUri, idTokenHint);
                _navigationManager.NavigateTo(logoutUrl);
            }
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error during logout");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Logout operation was canceled");
        }
    }

    #endregion

    #region Private Helper Methods - Configuration

    private (string authority, string clientId, string redirectUri) GetEntraConfiguration()
    {
        var authority = _configuration["MicrosoftEntraExternalId:Authority"];
        var clientId = _configuration["MicrosoftEntraExternalId:ClientId"];
        var redirectUri = _configuration["MicrosoftEntraExternalId:RedirectUri"];

        return (authority ?? string.Empty, clientId ?? string.Empty, redirectUri ?? string.Empty);
    }

    private static void ValidateEntraConfiguration(string? authority, string? clientId)
    {
        if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("Entra External ID is not configured. Missing Authority or ClientId.");
        }
    }

    #endregion

    #region Private Helper Methods - URL Building

    private static string BuildAuthorizationUrl(string authority, string clientId, string redirectUri, string state, string nonce, string codeChallenge, string? domainHint = null)
    {
        // Authority format: https://mystira.ciamlogin.com/{tenant_id}/v2.0
        // OAuth endpoint: https://mystira.ciamlogin.com/{tenant_id}/oauth2/v2.0/authorize

        // Remove trailing slash and /v2.0 from authority to build the correct base for endpoints
        var baseAuthority = authority.TrimEnd('/');
        if (baseAuthority.EndsWith("/v2.0"))
        {
            baseAuthority = baseAuthority.Substring(0, baseAuthority.Length - 5);
        }

        var scopes = string.Join(" ", DefaultScopes);

        var url = $"{baseAuthority}/oauth2/v2.0/authorize?" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"response_type=code&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"response_mode=fragment&" +
            $"scope={Uri.EscapeDataString(scopes)}&" +
            $"state={state}&" +
            $"nonce={nonce}&" +
            $"code_challenge={codeChallenge}&" +
            $"code_challenge_method=S256";

        // Add domain_hint to skip the Entra signin page and go directly to the identity provider
        if (!string.IsNullOrEmpty(domainHint))
        {
            url += $"&domain_hint={Uri.EscapeDataString(domainHint)}";
        }

        return url;
    }

    private static string BuildLogoutUrl(string authority, string postLogoutRedirectUri, string? idTokenHint = null)
    {
        // Authority format: https://mystira.ciamlogin.com/{tenant_id}/v2.0
        // Logout endpoint: https://mystira.ciamlogin.com/{tenant_id}/oauth2/v2.0/logout

        // Remove trailing slash and /v2.0 from authority
        var baseAuthority = authority.TrimEnd('/');
        if (baseAuthority.EndsWith("/v2.0"))
        {
            baseAuthority = baseAuthority.Substring(0, baseAuthority.Length - 5);
        }

        var url = $"{baseAuthority}/oauth2/v2.0/logout?" +
            $"post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

        if (!string.IsNullOrEmpty(idTokenHint))
        {
            url += $"&id_token_hint={Uri.EscapeDataString(idTokenHint)}";
        }

        return url;
    }

    #endregion

    #region Private Helper Methods - Security Tokens

    private async Task<(string state, string nonce, string codeVerifier)> GenerateAndStoreSecurityTokensAsync()
    {
        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");
        var codeVerifier = GenerateRandomString(64);

        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", AuthStateKey, state);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", AuthNonceKey, nonce);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", AuthCodeVerifierKey, codeVerifier);

        return (state, nonce, codeVerifier);
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    private static string Base64UrlEncode(byte[] arg)
    {
        var s = Convert.ToBase64String(arg); // Standard base64
        s = s.Split('=')[0]; // Remove any trailing '='s
        s = s.Replace('+', '-'); // 62nd char of encoding
        s = s.Replace('/', '_'); // 63rd char of encoding
        return s;
    }

    private async Task<(string AccessToken, string IdToken)?> ExchangeCodeForTokensAsync(string code)
    {
        try
        {
            var (authority, clientId, redirectUri) = GetEntraConfiguration();
            var codeVerifier = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", AuthCodeVerifierKey);

            if (string.IsNullOrEmpty(codeVerifier))
            {
                _logger.LogWarning("Code verifier missing from session storage");
                return null;
            }

            // Remove /v2.0 from authority
            var baseAuthority = authority.TrimEnd('/');
            if (baseAuthority.EndsWith("/v2.0"))
            {
                baseAuthority = baseAuthority.Substring(0, baseAuthority.Length - 5);
            }

            var tokenEndpoint = $"{baseAuthority}/oauth2/v2.0/token";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "code_verifier", codeVerifier },
                { "scope", string.Join(" ", DefaultScopes) }
            });

            _logger.LogInformation("Exchanging authorization code for tokens at: {TokenEndpoint}", tokenEndpoint);
            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken) || string.IsNullOrEmpty(tokenResponse.IdToken))
            {
                _logger.LogError("Invalid token response from Entra");
                return null;
            }

            return (tokenResponse.AccessToken, tokenResponse.IdToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token exchange");
            return null;
        }
    }

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private async Task<bool> ValidateStateAsync(Dictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("state", out var state))
        {
            return true; // State is optional
        }

        var storedState = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", AuthStateKey);
        return state == storedState;
    }

    private async Task ClearAuthStateAsync()
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", AuthStateKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", AuthNonceKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", AuthCodeVerifierKey);
    }

    #endregion

    #region Private Helper Methods - Storage

    private async Task LoadStoredAuthDataAsync()
    {
        _currentToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
        var accountJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccountStorageKey);

        if (!string.IsNullOrEmpty(accountJson))
        {
            _currentAccount = JsonSerializer.Deserialize<Account>(accountJson);
        }
    }

    private async Task SetStoredAccountAsync(Account account)
    {
        _currentAccount = account;
        var accountJson = JsonSerializer.Serialize(account);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccountStorageKey, accountJson);
    }

    private async Task StoreTokensAsync(string accessToken, string idToken)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, accessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", IdTokenStorageKey, idToken);
    }

    private async Task ClearLocalStorageAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", IdTokenStorageKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccountStorageKey);
    }

    private void ClearAuthenticationState()
    {
        _isAuthenticated = false;
        _currentAccount = null;
        _currentToken = null;
    }

    #endregion

    #region Private Helper Methods - Token Parsing

    private static bool TryExtractTokens(Dictionary<string, string> parameters, out string accessToken, out string idToken)
    {
        accessToken = string.Empty;
        idToken = string.Empty;

        if (!parameters.TryGetValue("access_token", out accessToken!))
        {
            return false;
        }

        if (!parameters.TryGetValue("id_token", out idToken!))
        {
            return false;
        }

        return true;
    }

    private Account? ExtractAccountFromIdToken(string idToken)
    {
        try
        {
            var claims = DecodeJwtPayload(idToken);
            if (claims == null)
            {
                return null;
            }

            var email = ExtractClaim(claims, "email", "preferred_username");
            var name = ExtractClaim(claims, "name", "given_name") ?? email;
            var sub = ExtractClaim(claims, "sub");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sub))
            {
                _logger.LogWarning("Email or subject missing from ID token");
                return null;
            }

            return new Account
            {
                Id = Guid.NewGuid().ToString(), // Will be mapped to actual account ID by API
                Email = email,
                DisplayName = name ?? email,
                CreatedAt = DateTime.UtcNow,
            };
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid JWT format when extracting account from ID token");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error extracting account from ID token");
            return null;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when extracting account from ID token");
            return null;
        }
    }

    private Dictionary<string, JsonElement>? DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            _logger.LogWarning("Invalid JWT format: expected 3 parts, got {Count}", parts.Length);
            return null;
        }

        var payload = parts[1];

        // Add padding if needed
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        var payloadBytes = Convert.FromBase64String(payload);
        var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
    }

    private static string? ExtractClaim(Dictionary<string, JsonElement> claims, params string[] claimNames)
    {
        foreach (var claimName in claimNames)
        {
            if (claims.TryGetValue(claimName, out var claim))
            {
                return claim.GetString();
            }
        }

        return null;
    }

    #endregion

    #region Private Helper Methods - URL Parsing

    private async Task<string?> GetUrlFragmentAsync()
    {
        var fragment = await _jsRuntime.InvokeAsync<string>("eval", "window.location.hash");

        if (string.IsNullOrEmpty(fragment) || !fragment.StartsWith("#"))
        {
            return null;
        }

        return fragment.Substring(1);
    }

    private static Dictionary<string, string> ParseFragment(string fragment)
    {
        var parameters = new Dictionary<string, string>();

        foreach (var pair in fragment.Split('&'))
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                parameters[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return parameters;
    }

    #endregion

    #region Private Helper Methods - Navigation

    private async Task<string> GetCurrentOriginAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to get current origin, using fallback");
            return "https://mystira.app"; // Fallback
        }
    }

    #endregion
}
