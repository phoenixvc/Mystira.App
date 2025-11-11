using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private const string DemoTokenPrefix = "demo_token_";

    private bool _isAuthenticated;
    private string? _currentToken;
    private Account? _currentAccount;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public AuthService(ILogger<AuthService> logger)
    {
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

    public Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting login for email: {Email}", email);

            // Demo authentication until real API is connected
            var demoAccount = new Account
            {
                Auth0UserId = $"demo|{Guid.NewGuid():N}",
                Email = email,
                DisplayName = email.Split('@')[0]
            };

            var demoToken = $"{DemoTokenPrefix}{Guid.NewGuid():N}";

            SetStoredToken(demoToken);
            SetStoredAccount(demoAccount);

            _isAuthenticated = true;
            _currentAccount = demoAccount;

            _logger.LogInformation("Login successful for: {Email}", email);
            AuthenticationStateChanged?.Invoke(this, true);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return Task.FromResult(false);
        }
    }

    public Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user");

            ClearStoredToken();
            ClearStoredAccount();

            _isAuthenticated = false;
            _currentAccount = null;

            _logger.LogInformation("Logout successful");
            AuthenticationStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }

        return Task.CompletedTask;
    }

    private Task<string?> GetStoredTokenAsync()
    {
        return Task.FromResult(_currentToken);
    }

    private void SetStoredToken(string token)
    {
        _currentToken = token;
    }

    private void ClearStoredToken()
    {
        _currentToken = null;
    }

    private Task<Account?> GetStoredAccountAsync()
    {
        return Task.FromResult(_currentAccount);
    }

    private void SetStoredAccount(Account account)
    {
        _currentAccount = account;
    }

    private void ClearStoredAccount()
    {
        _currentAccount = null;
    }
}
