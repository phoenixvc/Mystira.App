using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

public class PlayerContextService : IPlayerContextService
{
    private const string SelectedProfileIdStorageKey = "mystira_selected_profile_id";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PlayerContextService> _logger;

    public PlayerContextService(IJSRuntime jsRuntime, ILogger<PlayerContextService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<string?> GetSelectedProfileIdAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SelectedProfileIdStorageKey);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read selected profile id from localStorage");
            return null;
        }
    }

    public async Task SetSelectedProfileIdAsync(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SelectedProfileIdStorageKey, profileId);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to store selected profile id in localStorage");
        }
    }

    public async Task ClearSelectedProfileIdAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SelectedProfileIdStorageKey);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to clear selected profile id from localStorage");
        }
    }
}
