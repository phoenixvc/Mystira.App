using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IApiClient _apiClient;
    private const string DemoTokenPrefix = "demo_token_";

    private bool _isAuthenticated;
    private string? _currentToken;
    private Account? _currentAccount;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public AuthService(ILogger<AuthService> logger, IApiClient apiClient)
    {
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

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await GetStoredTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token");
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
            return (false, response?.Message ?? "Failed to request signup code");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting passwordless signup for: {Email}", email);
            return (false, "An error occurred while processing your request");
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
                SetStoredToken(response.Token ?? $"{DemoTokenPrefix}{Guid.NewGuid():N}");
                
                // Fetch full account details from API
                var fullAccount = await _apiClient.GetAccountByEmailAsync(email);
                if (fullAccount != null)
                {
                    SetStoredAccount(fullAccount);
                    _isAuthenticated = true;
                    _logger.LogInformation("Passwordless signup verified successfully for: {Email}", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "Account created successfully", fullAccount);
                }
                else
                {
                    // Fallback to response account if API call fails
                    SetStoredAccount(response.Account);
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
                SetStoredToken(response.Token ?? $"{DemoTokenPrefix}{Guid.NewGuid():N}");
                
                // Fetch full account details from API
                var fullAccount = await _apiClient.GetAccountByEmailAsync(email);
                if (fullAccount != null)
                {
                    SetStoredAccount(fullAccount);
                    _isAuthenticated = true;
                    _logger.LogInformation("Passwordless signin verified successfully for: {Email}", email);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return (true, "Sign-in successful", fullAccount);
                }
                else
                {
                    // Fallback to response account if API call fails
                    SetStoredAccount(response.Account);
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

    private void ClearStoredAccount()
    {
        _currentAccount = null;
    }
}
