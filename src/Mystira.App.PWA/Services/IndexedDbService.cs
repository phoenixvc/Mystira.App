using System.Text.Json;
using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

public class IndexedDbService : IIndexedDbService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<IndexedDbService> _logger;

    public IndexedDbService(IJSRuntime jsRuntime, ILogger<IndexedDbService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string storeName, string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"{storeName}:{key}");
            
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from IndexedDB: {StoreName}:{Key}", storeName, key);
            return default;
        }
    }

    public async Task<bool> SetAsync<T>(string storeName, string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"{storeName}:{key}", json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in IndexedDB: {StoreName}:{Key}", storeName, key);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string storeName, string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{storeName}:{key}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item from IndexedDB: {StoreName}:{Key}", storeName, key);
            return false;
        }
    }

    public async Task<List<string>> GetKeysAsync(string storeName)
    {
        try
        {
            var keys = new List<string>();
            var length = await _jsRuntime.InvokeAsync<int>("localStorage.length");
            
            for (int i = 0; i < length; i++)
            {
                var key = await _jsRuntime.InvokeAsync<string>("localStorage.key", i);
                if (key != null && key.StartsWith($"{storeName}:"))
                {
                    keys.Add(key.Substring($"{storeName}:".Length));
                }
            }
            
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys from IndexedDB for store: {StoreName}", storeName);
            return new List<string>();
        }
    }
}
