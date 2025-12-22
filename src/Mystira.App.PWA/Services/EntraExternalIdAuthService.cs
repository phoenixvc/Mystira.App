using System.Text.Json;
using Microsoft.JSInterop;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Authentication service for Microsoft Entra External ID (CIAM)
/// Supports Google social login and email+password authentication
/// </summary>
public class EntraExternalIdAuthService : IAuthService
{
    private readonly ILogger<EntraExternalIdAuthService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    
    private const string TokenStorageKey = "mystira_entra_token";
    private const string AccountStorageKey = "mystira_entra_account";
    private const string IdTokenStorageKey = "mystira_entra_id_token";
    
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
        IConfiguration configuration)
    {
        _logger = logger;
        _apiClient = apiClient;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            if (_isAuthenticated && !string.IsNullOrEmpty(_currentToken) && _currentAccount != null)
            {
                return true;
            }

            await LoadStoredAuthData();
            _isAuthenticated = !string.IsNullOrEmpty(_currentToken) && _currentAccount != null;
            
            return _isAuthenticated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public async Task<Account?> GetCurrentAccountAsync()
    {
        try
        {
            if (_currentAccount == null)
            {
                await LoadStoredAuthData();
            }

            return _currentAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current account");
            return null;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentToken))
            {
                await LoadStoredAuthData();
            }

            return _currentToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token");
            return null;
        }
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

    /// <summary>
    /// Initiates login flow with Microsoft Entra External ID
    /// Redirects to Entra login page which supports Google social login
    /// </summary>
    public async Task LoginWithEntraAsync()
    {
        try
        {
            _logger.LogInformation("Initiating Entra External ID login");
            
            var authority = _configuration["MicrosoftEntraExternalId:Authority"];
            var clientId = _configuration["MicrosoftEntraExternalId:ClientId"];
            var redirectUri = _configuration["MicrosoftEntraExternalId:RedirectUri"] 
                ?? $"{await GetCurrentOriginAsync()}/authentication/login-callback";
            
            if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Entra External ID configuration missing");
                throw new InvalidOperationException("Entra External ID is not configured");
            }

            // Construct authorization URL
            var scopes = string.Join(" ", new[] 
            { 
                "openid", 
                "profile", 
                "email",
                "offline_access" // For refresh tokens
            });
            
            var state = Guid.NewGuid().ToString("N");
            var nonce = Guid.NewGuid().ToString("N");
            
            // Store state and nonce for validation
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "entra_auth_state", state);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "entra_auth_nonce", nonce);
            
            var authUrl = $"{authority}/oauth2/v2.0/authorize?" +
                $"client_id={Uri.EscapeDataString(clientId)}&" +
                $"response_type=id_token token&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                $"response_mode=fragment&" +
                $"scope={Uri.EscapeDataString(scopes)}&" +
                $"state={state}&" +
                $"nonce={nonce}";
            
            _logger.LogInformation("Redirecting to Entra External ID login");
            await _jsRuntime.InvokeVoidAsync("window.location.href", authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Entra External ID login");
            throw;
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
            
            // Extract tokens from URL fragment
            var fragment = await _jsRuntime.InvokeAsync<string>("eval", "window.location.hash");
            
            if (string.IsNullOrEmpty(fragment) || !fragment.StartsWith("#"))
            {
                _logger.LogWarning("No fragment found in callback URL");
                return false;
            }

            var parameters = ParseFragment(fragment.Substring(1));
            
            if (!parameters.TryGetValue("access_token", out var accessToken) ||
                !parameters.TryGetValue("id_token", out var idToken))
            {
                _logger.LogWarning("Access token or ID token missing from callback");
                return false;
            }

            // Validate state
            if (parameters.TryGetValue("state", out var state))
            {
                var storedState = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "entra_auth_state");
                if (state != storedState)
                {
                    _logger.LogWarning("State mismatch in callback");
                    return false;
                }
            }

            // Store tokens
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", IdTokenStorageKey, idToken);
            
            _currentToken = accessToken;

            // Extract user info from ID token
            var account = await ExtractAccountFromIdToken(idToken);
            if (account != null)
            {
                await SetStoredAccount(account);
                _isAuthenticated = true;
                
                _logger.LogInformation("Entra External ID login successful for: {Email}", account.Email);
                AuthenticationStateChanged?.Invoke(this, true);
                
                // Clear auth state from session storage
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "entra_auth_state");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "entra_auth_nonce");
                
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Entra External ID login callback");
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

            // Clear local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", IdTokenStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccountStorageKey);

            _isAuthenticated = false;
            _currentAccount = null;
            _currentToken = null;

            _logger.LogInformation("Local logout successful");
            AuthenticationStateChanged?.Invoke(this, false);

            // Redirect to Entra logout endpoint
            if (!string.IsNullOrEmpty(authority))
            {
                var logoutUrl = $"{authority}/oauth2/v2.0/logout?" +
                    $"post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";
                
                await _jsRuntime.InvokeVoidAsync("window.location.href", logoutUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
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

    public async Task<string?> GetCurrentTokenAsync()
    {
        return await GetTokenAsync();
    }

    public DateTime? GetTokenExpiryTime()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentToken))
            {
                return null;
            }

            // Decode JWT to get expiry
            var parts = _currentToken.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var payload = parts[1];
            // Add padding if needed
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var payloadBytes = Convert.FromBase64String(payload);
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

            if (claims != null && claims.TryGetValue("exp", out var expClaim))
            {
                var exp = expClaim.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token expiry time");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring token validity");
            return false;
        }
    }

    private async Task LoadStoredAuthData()
    {
        try
        {
            _currentToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
            var accountJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccountStorageKey);

            if (!string.IsNullOrEmpty(accountJson))
            {
                _currentAccount = JsonSerializer.Deserialize<Account>(accountJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stored auth data");
        }
    }

    private async Task SetStoredAccount(Account account)
    {
        try
        {
            _currentAccount = account;
            var accountJson = JsonSerializer.Serialize(account);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccountStorageKey, accountJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing account data");
        }
    }

    private async Task<Account?> ExtractAccountFromIdToken(string idToken)
    {
        try
        {
            // Decode JWT (simple base64 decode of payload)
            var parts = idToken.Split('.');
            if (parts.Length != 3)
            {
                _logger.LogWarning("Invalid ID token format");
                return null;
            }

            var payload = parts[1];
            // Add padding if needed
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var payloadBytes = Convert.FromBase64String(payload);
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

            if (claims == null)
            {
                return null;
            }

            var email = claims.TryGetValue("email", out var emailClaim) 
                ? emailClaim.GetString() 
                : claims.TryGetValue("preferred_username", out var usernameClaim)
                    ? usernameClaim.GetString()
                    : null;

            var name = claims.TryGetValue("name", out var nameClaim)
                ? nameClaim.GetString()
                : claims.TryGetValue("given_name", out var givenNameClaim)
                    ? givenNameClaim.GetString()
                    : email;

            var sub = claims.TryGetValue("sub", out var subClaim)
                ? subClaim.GetString()
                : null;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sub))
            {
                _logger.LogWarning("Email or subject missing from ID token");
                return null;
            }

            // Create account from Entra External ID user
            return new Account
            {
                Id = Guid.NewGuid(), // Will be mapped to actual account ID by API
                Email = email,
                DisplayName = name ?? email,
                ExternalId = sub, // Store Entra External ID subject
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting account from ID token");
            return null;
        }
    }

    private Dictionary<string, string> ParseFragment(string fragment)
    {
        var parameters = new Dictionary<string, string>();
        
        foreach (var pair in fragment.Split('&'))
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                parameters[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return parameters;
    }

    private async Task<string> GetCurrentOriginAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
        }
        catch
        {
            return "https://mystira.app"; // Fallback
        }
    }
}
