namespace Mystira.App.PWA.Services;

public interface IPlayerContextService
{
    Task<string?> GetSelectedProfileIdAsync();
    Task SetSelectedProfileIdAsync(string profileId);
    Task ClearSelectedProfileIdAsync();
}
