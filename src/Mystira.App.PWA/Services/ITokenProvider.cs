namespace Mystira.App.PWA.Services;

public interface ITokenProvider
{
    Task<string?> GetCurrentTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}
