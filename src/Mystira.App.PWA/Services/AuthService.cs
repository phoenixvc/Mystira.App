using System.Text.Json;
using Microsoft.JSInterop;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IJSRuntime _jsRuntime;
    private const string DemoTokenPrefix = "demo_token_";
    private const string TokenStorageKey = "mystira_auth_token";
    private const string RefreshTokenStorageKey = "mystira_refresh_token";
    private const string AccountStorageKey = "mystira_account";
    private const string TokenExpiryStorageKey = "mystira_token_expiry";

    private bool _isAuthenticated;
    private string? _currentToken;
    private string? _currentRefreshToken;
    private Account? _currentAccount;
    private DateTime? _tokenExpiryTime;
    private bool _rememberMe = false;
    private bool _isRefreshing = false;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public event EventHandler<bool>? AuthenticationStateChanged;
    public event EventHandler? TokenExpiryWarning;

    public AuthService(ILogger<AuthService> logger, IApiClient apiClient, IJSRuntime jsRuntime)
    {
        _logger = logger;
        _apiClient = apiClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            // If we already have valid authentication state, return it
            if (_isAuthenticated && !string.IsNullOrEmpty(_currentToken) && _currentAccount != null)
            {
                // Check if token is expired by trying to decode it
                if (IsTokenExpired(_currentToken))
                {
                    _logger.LogInformation("Access token expired, attempting refresh");
                    var refreshSuccess = await RefreshTokenIfNeeded();
                    if (!refreshSuccess)
                    {
                        _logger.LogWarning("Token refresh failed, logging out");
                        await LogoutAsync();
                        return false;
                    }
                }
                return true;
            }

            // Try to load from storage
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
            return await GetCurrentTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token");
            return null;
        }
    }

    public void SetRememberMe(bool rememberMe)
    {
        _rememberMe = rememberMe;
    }

    public Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting login for email: {Email}", email);

            // Login not implemented - use passwordless authentication methods instead
            _logger.LogWarning("LoginAsync called with email: {Email}, but is not implemented. Use passwordless methods instead.", email);

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return Task.FromResult(false);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user");

            await ClearStoredToken();
            await ClearStoredRefreshToken();
            await ClearStoredAccount();

            _isAuthenticated = false;
            _currentAccount = null;
            _currentToken = null;
            _currentRefreshToken = null;

            _logger.LogInformation("Logout successful");
            AuthenticationStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<(bool Success, string Message)> RequestPasswordlessSignupAsync(string email, string displayName)
    {
        try
        {
            _logger.LogInformation("Requesting passwordless signup for: {Email}", email);

            var response = await _apiClient.RequestPasswordlessSignupAsync(email, displayName);

            if (response?.Success == true)
            {
                _logger.LogInformation("Passwordless signup requested successfully for: {Email}", email);
                return (true, response.Message);
            }

            _logger.LogWarning("Passwordless signup request failed for: {Email}", email);
            return (false, response?.Message ?? "Failed to request signup code. Please check your connection and try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error requesting passwordless signup for: {Email}", email);
            return (false, "Unable to connect to the server. Please check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting passwordless signup for: {Email}", email);
            return (false, "An error occurred while processing your request. Please try again.");
        }
    }

    public async Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSignupAsync(string email, string code)
    {
        try
        {
            _logger.LogInformation("Verifying passwordless signup for: {Email}", email);

            var response = await _apiClient.VerifyPasswordlessSignupAsync(email, code);

            if (response?.Success == true && response.Account != null)
            {
                await SetStoredToken(response.Token ?? $"{DemoTokenPrefix}{Guid.NewGuid():N}");
                await SetStoredRefreshToken(response.RefreshToken, _rememberMe);

                // Fetch full account details from API
                var fullAccount = await _apiClient.GetAccountByEmailAsync(email);
                if (fullAccount != null)
                {
                    await SetStoredAccount(fullAccount);
                    _isAuthenticated = true;
                    _logger.LogInformation("Passwordless signup verified successfully for: {Email}", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "Account created successfully", fullAccount);
                }
                else
                {
                    // Fallback to response account if API call fails
                    await SetStoredAccount(response.Account);
                    _isAuthenticated = true;
                    _logger.LogInformation("Passwordless signup verified successfully for: {Email}", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "Account created successfully", response.Account);
                }
            }

            _logger.LogWarning("Passwordless signup verification failed for: {Email}", email);
            return (false, response?.Message ?? "Verification failed", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passwordless signup for: {Email}", email);
            return (false, "An error occurred during verification", null);
        }
    }

    private async Task LoadStoredAuthData()
    {
        try
        {
            _currentToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
            // Always load refresh token from localStorage (fix for session storage loss on page refresh)
            _currentRefreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenStorageKey);
            var accountJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccountStorageKey);
            var expiryString = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenExpiryStorageKey);

            if (!string.IsNullOrEmpty(accountJson))
            {
                _currentAccount = JsonSerializer.Deserialize<Account>(accountJson);
            }

            if (!string.IsNullOrEmpty(expiryString) && DateTime.TryParse(expiryString, out var expiry))
            {
                _tokenExpiryTime = expiry;
            }
            else if (!string.IsNullOrEmpty(_currentToken))
            {
                // Extract expiry from token if not stored
                _tokenExpiryTime = GetExpiryFromToken(_currentToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stored auth data");
        }
    }

    private DateTime? GetExpiryFromToken(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    private async Task SetStoredToken(string? token)
    {
        _currentToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, token);
            // Store token expiry time
            _tokenExpiryTime = GetExpiryFromToken(token);
            if (_tokenExpiryTime.HasValue)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpiryStorageKey, _tokenExpiryTime.Value.ToString("O"));
            }
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenExpiryStorageKey);
            _tokenExpiryTime = null;
        }
    }

    private async Task SetStoredRefreshToken(string? refreshToken, bool rememberMe = false)
    {
        _currentRefreshToken = refreshToken;
        // Always store in localStorage to prevent loss on page refresh
        // The rememberMe flag is kept for API compatibility but localStorage is always used
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenStorageKey, refreshToken);
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenStorageKey);
        }
    }

    private async Task ClearStoredToken()
    {
        _currentToken = null;
        _tokenExpiryTime = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenExpiryStorageKey);
    }

    private async Task ClearStoredRefreshToken()
    {
        _currentRefreshToken = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenStorageKey);
    }

    private async Task SetStoredAccount(Account? account)
    {
        _currentAccount = account;
        if (account != null)
        {
            var accountJson = JsonSerializer.Serialize(account);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccountStorageKey, accountJson);
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccountStorageKey);
        }
    }

    private async Task ClearStoredAccount()
    {
        _currentAccount = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccountStorageKey);
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            return jwtToken.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            // If we can't parse the token, consider it expired
            return true;
        }
    }

    private async Task<bool> RefreshTokenIfNeeded()
    {
        if (string.IsNullOrEmpty(_currentRefreshToken) || string.IsNullOrEmpty(_currentToken))
        {
            return false;
        }

        try
        {
            var (success, message, newToken, newRefreshToken) = await RefreshTokenAsync(_currentToken, _currentRefreshToken);

            if (success && !string.IsNullOrEmpty(newToken))
            {
                await SetStoredToken(newToken);
                await SetStoredRefreshToken(newRefreshToken);
                _logger.LogInformation("Token refreshed successfully");
                return true;
            }

            _logger.LogWarning("Token refresh failed: {Message}", message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }

    public async Task<(bool Success, string Message)> RequestPasswordlessSigninAsync(string email)
    {
        try
        {
            _logger.LogInformation("Requesting passwordless signin for: {Email}", email);

            var response = await _apiClient.RequestPasswordlessSigninAsync(email);

            if (response?.Success == true)
            {
                _logger.LogInformation("Passwordless signin requested successfully for: {Email}", email);
                return (true, response.Message);
            }

            _logger.LogWarning("Passwordless signin request failed for: {Email}", email);
            return (false, response?.Message ?? "Failed to request signin code");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting passwordless signin for: {Email}", email);
            return (false, "An error occurred while processing your request");
        }
    }

    public async Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSigninAsync(string email, string code)
    {
        try
        {
            _logger.LogInformation("Verifying passwordless signin for: {Email}", email);

            var response = await _apiClient.VerifyPasswordlessSigninAsync(email, code);

            if (response?.Success == true && response.Account != null)
            {
                await SetStoredToken(response.Token ?? $"{DemoTokenPrefix}{Guid.NewGuid():N}");
                await SetStoredRefreshToken(response.RefreshToken, _rememberMe);

                // Fetch full account details from API
                var fullAccount = await _apiClient.GetAccountByEmailAsync(email);
                if (fullAccount != null)
                {
                    await SetStoredAccount(fullAccount);
                    _isAuthenticated = true;
                    _logger.LogInformation("Passwordless signin verified successfully for: {Email}", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "Sign-in successful", fullAccount);
                }
                else
                {
                    // Fallback to response account if API call fails
                    await SetStoredAccount(response.Account);
                    _isAuthenticated = true;
                    _logger.LogInformation("Passwordless signin verified successfully for: {Email}", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "Sign-in successful", response.Account);
                }
            }

            _logger.LogWarning("Passwordless signin verification failed for: {Email}", email);
            return (false, response?.Message ?? "Verification failed", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passwordless signin for: {Email}", email);
            return (false, "An error occurred during verification", null);
        }
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken)
    {
        try
        {
            _logger.LogInformation("Requesting token refresh");

            var response = await _apiClient.RefreshTokenAsync(token, refreshToken);

            if (response?.Success == true)
            {
                await SetStoredToken(response.Token);
                await SetStoredRefreshToken(response.RefreshToken, _rememberMe); // Use current remember me setting
                _logger.LogInformation("Token refreshed successfully");
                return (true, response.Message, response.Token, response.RefreshToken);
            }

            _logger.LogWarning("Token refresh failed: {Message}", response?.Message);
            return (false, response?.Message ?? "Token refresh failed", null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return (false, "An error occurred while refreshing token", null, null);
        }
    }

    public async Task<string?> GetCurrentTokenAsync()
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
            _logger.LogError(ex, "Error getting current token");
            return null;
        }
    }

    public DateTime? GetTokenExpiryTime()
    {
        if (_tokenExpiryTime.HasValue)
        {
            return _tokenExpiryTime;
        }

        if (!string.IsNullOrEmpty(_currentToken))
        {
            _tokenExpiryTime = GetExpiryFromToken(_currentToken);
        }

        return _tokenExpiryTime;
    }

    public async Task<bool> EnsureTokenValidAsync(int expiryBufferMinutes = 5)
    {
        // Prevent concurrent refresh attempts
        await _refreshLock.WaitAsync();
        try
        {
            if (_isRefreshing)
            {
                _logger.LogDebug("Token refresh already in progress, waiting...");
                return true; // Another refresh is in progress
            }

            if (string.IsNullOrEmpty(_currentToken))
            {
                await LoadStoredAuthData();
            }

            if (string.IsNullOrEmpty(_currentToken))
            {
                return false; // No token available
            }

            var expiry = GetTokenExpiryTime();
            if (!expiry.HasValue)
            {
                return true; // Can't determine expiry, assume valid
            }

            var timeUntilExpiry = expiry.Value - DateTime.UtcNow;

            // Check if token is about to expire (within buffer time)
            if (timeUntilExpiry.TotalMinutes <= expiryBufferMinutes)
            {
                // Raise warning event if within 5 minutes of expiry
                if (timeUntilExpiry.TotalMinutes <= 5 && timeUntilExpiry.TotalMinutes > 0)
                {
                    _logger.LogInformation("Token expiring in {Minutes} minutes, raising warning", timeUntilExpiry.TotalMinutes);
                    TokenExpiryWarning?.Invoke(this, EventArgs.Empty);
                }

                // If already expired or about to expire, try to refresh
                if (string.IsNullOrEmpty(_currentRefreshToken))
                {
                    _logger.LogWarning("Token expired and no refresh token available");
                    return false;
                }

                _isRefreshing = true;
                try
                {
                    _logger.LogInformation("Proactively refreshing token before expiry");
                    var (success, message, newToken, newRefreshToken) = await RefreshTokenAsync(_currentToken, _currentRefreshToken);

                    if (success && !string.IsNullOrEmpty(newToken))
                    {
                        _logger.LogInformation("Proactive token refresh successful");
                        return true;
                    }

                    _logger.LogWarning("Proactive token refresh failed: {Message}", message);
                    return false;
                }
                finally
                {
                    _isRefreshing = false;
                }
            }

            return true; // Token is still valid
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
