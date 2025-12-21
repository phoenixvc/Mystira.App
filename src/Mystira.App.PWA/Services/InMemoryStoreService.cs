using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace Mystira.App.PWA.Services;

public class InMemoryStoreService : IInMemoryStoreService
{
    private readonly ILogger<InMemoryStoreService> _logger;
    private readonly IJSRuntime _jsRuntime;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _stores = new();
    private bool _initialized = false;

    public InMemoryStoreService(ILogger<InMemoryStoreService> logger, IJSRuntime jsRuntime)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    private async Task EnsureInitializedAsync(string storeName)
    {
        if (_initialized) return;

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"mystira_store_{storeName}");
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(json);
                if (data != null)
                {
                    _stores[storeName] = data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load store {StoreName} from localStorage", storeName);
        }

        _initialized = true;
    }

    private async Task PersistAsync(string storeName)
    {
        try
        {
            if (_stores.TryGetValue(storeName, out var store))
            {
                var json = JsonSerializer.Serialize(store);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"mystira_store_{storeName}", json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist store {StoreName} to localStorage", storeName);
        }
    }

    public async Task<T?> GetAsync<T>(string storeName, string key)
    {
        await EnsureInitializedAsync(storeName);

        try
        {
            if (_stores.TryGetValue(storeName, out var store) &&
                store.TryGetValue(key, out var json))
            {
                return JsonSerializer.Deserialize<T>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from store: {StoreName}:{Key}", storeName, key);
        }

        return default;
    }

    public async Task<bool> SetAsync<T>(string storeName, string key, T value)
    {
        await EnsureInitializedAsync(storeName);

        try
        {
            var store = _stores.GetOrAdd(storeName, static _ => new ConcurrentDictionary<string, string>());
            var json = JsonSerializer.Serialize(value);
            store[key] = json;

            await PersistAsync(storeName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in store: {StoreName}:{Key}", storeName, key);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string storeName, string key)
    {
        await EnsureInitializedAsync(storeName);

        try
        {
            if (_stores.TryGetValue(storeName, out var store))
            {
                store.TryRemove(key, out _);
                await PersistAsync(storeName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item from store: {StoreName}:{Key}", storeName, key);
            return false;
        }
    }

    public async Task<List<string>> GetKeysAsync(string storeName)
    {
        await EnsureInitializedAsync(storeName);

        try
        {
            if (_stores.TryGetValue(storeName, out var store))
            {
                return store.Keys.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys from store for: {StoreName}", storeName);
        }

        return new List<string>();
    }

    public async Task ClearAsync(string storeName)
    {
        try
        {
            _stores.TryRemove(storeName, out _);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"mystira_store_{storeName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing store: {StoreName}", storeName);
        }
    }
}
