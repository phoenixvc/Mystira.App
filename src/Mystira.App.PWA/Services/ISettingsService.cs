namespace Mystira.App.PWA.Services;

public interface ISettingsService
{
    Task<bool> GetShowAgeGroupMismatchWarningAsync();
    Task SetShowAgeGroupMismatchWarningAsync(bool value);
    Task<bool> GetShowGameAlreadyPlayedWarningAsync();
    Task SetShowGameAlreadyPlayedWarningAsync(bool value);
}
