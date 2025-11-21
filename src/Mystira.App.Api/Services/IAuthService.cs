namespace Mystira.App.Api.Services;

public interface IAuthService
{
    string? AuthToken { get; }
    bool IsAuthenticated { get; }
    Task<bool> LoginAsync(string username, string password);
    void Logout();
}
