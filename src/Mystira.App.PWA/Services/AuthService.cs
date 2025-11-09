using System.Text.Json;
using Microsoft.JSInterop;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthService> _logger;
    private readonly IApiClient _apiClient;
    private const string AUTH_TOKEN_KEY = "mystira_auth_token";
    private const string ACCOUNT_KEY = "mystira_account";
    
    private bool _isAuthenticated = false;
    private Account? _currentAccount = null;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public AuthService(IJSRuntime jsRuntime, ILogger<AuthService> logger, IApiClient apiClient)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _apiClient = apiClient;
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

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting login for email: {Email}", email);
            
            // For demo purposes, create a simple demo authentication
            // In a real implementation, this would call an authentication API
            var demoAccount = new Account
            {
                Auth0UserId = $"demo|{Guid.NewGuid():N}",
                Email = email,
                DisplayName = email.Split('@')[0]
            };
            
            var demoToken = $"demo_token_{Guid.NewGuid():N}";
            
            // Store token and account in localStorage
            await SetStoredTokenAsync(demoToken);
            await SetStoredAccountAsync(demoAccount);
            
            _isAuthenticated = true;
            _currentAccount = demoAccount;
            
            _logger.LogInformation("Login successful for: {Email}", email);
            AuthenticationStateChanged?.Invoke(this, true);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
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
            
            _logger.LogInformation("Logout successful");
            AuthenticationStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
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
