using Microsoft.JSInterop;
using Mystira.App.PWA.Models;
using System.Text.Json;

namespace Mystira.App.PWA.Services;

public class SimpleAuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<SimpleAuthService> _logger;
    private const string AUTH_TOKEN_KEY = "mystira_auth_token";
    private const string ACCOUNT_KEY = "mystira_account";
    
    private bool _isAuthenticated = false;
    private Account? _currentAccount = null;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public SimpleAuthService(IJSRuntime jsRuntime, ILogger<SimpleAuthService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await GetStoredTokenAsync();
            var account = await GetStoredAccountAsync();
            
            _isAuthenticated = !string.IsNullOrEmpty(token) && account != null;
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
                _currentAccount = await GetStoredAccountAsync();
            }
            return _currentAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current account");
            return null;
        }
    }

    public async Task LoginAsync(bool rememberMe = false, string? connection = null)
    {
        try
        {
            _logger.LogInformation("Initiating login with rememberMe: {RememberMe}, connection: {Connection}", rememberMe, connection);
            
            // For demo purposes, redirect to Auth0 login page
            var loginUrl = "/authentication/login";
            if (!string.IsNullOrEmpty(connection))
            {
                LoginUrl += "?connection=" + connection;
            }
            if (rememberMe)
            {
                LoginUrl += "&prompt=consent&scope=openid profile email offline_access";
            }
            
            await _jsRuntime.InvokeVoidAsync("window.location.href", LoginUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login initiation");
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user");
            
            // Clear stored data
            await ClearStoredTokenAsync();
            await ClearStoredAccountAsync();
            
            _isAuthenticated = false;
            _currentAccount = null;
            
            // Redirect to logout page
            await _jsRuntime.InvokeVoidAsync("window.location.href", "/authentication/logout");
            
            _logger.LogInformation("Logout successful");
            AuthenticationStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await GetStoredTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token");
            return null;
        }
    }

    private async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AUTH_TOKEN_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stored token");
            return null;
        }
    }

    private async Task<Account?> GetStoredAccountAsync()
    {
        try
        {
            var accountJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ACCOUNT_KEY);
            if (string.IsNullOrEmpty(accountJson))
                return null;
                
            return JsonSerializer.Deserialize<Account>(accountJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stored account");
            return null;
        }
    }

    private async Task SetStoredTokenAsync(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AUTH_TOKEN_KEY, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting stored token");
        }
    }

    private async Task SetStoredAccountAsync(Account account)
    {
        try
        {
            var accountJson = JsonSerializer.Serialize(account);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ACCOUNT_KEY, accountJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting stored account");
        }
    }

    private async Task ClearStoredTokenAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AUTH_TOKEN_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing stored token");
        }
    }

    private async Task ClearStoredAccountAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ACCOUNT_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing stored account");
        }
    }
}