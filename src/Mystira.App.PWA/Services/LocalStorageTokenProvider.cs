using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

public class LocalStorageTokenProvider : ITokenProvider
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenStorageKey = "auth_token";
    private const string AccountStorageKey = "auth_account";

    public LocalStorageTokenProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var accountJson = await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem", AccountStorageKey);

        return !string.IsNullOrWhiteSpace(accountJson);
    }

    public async Task<string?> GetCurrentTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem", TokenStorageKey);
    }
}