using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<Account?> GetCurrentAccountAsync();
    Task LoginAsync(bool rememberMe = false, string? connection = null);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    event EventHandler<bool>? AuthenticationStateChanged;
}
