using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<Account?> GetCurrentAccountAsync();
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();
    event EventHandler<bool>? AuthenticationStateChanged;
}
