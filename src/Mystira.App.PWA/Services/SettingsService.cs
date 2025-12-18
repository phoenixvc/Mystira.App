using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

public class SettingsService : ISettingsService
{
    private readonly IJSRuntime _jsRuntime;
    private const string ShowAgeGroupMismatchWarningKey = "mystira_show_age_group_warning";
    private const string ShowGameAlreadyPlayedWarningKey = "mystira_show_game_already_played_warning";
    private const string ShowGuestWarningKey = "mystira_show_guest_warning";

    public SettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> GetShowAgeGroupMismatchWarningAsync()
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ShowAgeGroupMismatchWarningKey);
            // Default to true if not set
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    public async Task SetShowAgeGroupMismatchWarningAsync(bool value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ShowAgeGroupMismatchWarningKey, value.ToString().ToLower());
        }
        catch
        {
            // Silently fail if localStorage is not available
        }
    }

    public async Task<bool> GetShowGameAlreadyPlayedWarningAsync()
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ShowGameAlreadyPlayedWarningKey);
            // Default to true if not set
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    public async Task SetShowGameAlreadyPlayedWarningAsync(bool value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ShowGameAlreadyPlayedWarningKey, value.ToString().ToLower());
        }
        catch
        {
            // Silently fail if localStorage is not available
        }
    }

    public async Task<bool> GetShowGuestWarningAsync()
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ShowGuestWarningKey);
            // Default to true if not set
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    public async Task SetShowGuestWarningAsync(bool value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ShowGuestWarningKey, value.ToString().ToLower());
        }
        catch
        {
            // Silently fail if localStorage is not available
        }
    }
}
