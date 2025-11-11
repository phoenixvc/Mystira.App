using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Mystira.App.PWA.Services;

public class IndexedDbService : IIndexedDbService
{
    private readonly ILogger<IndexedDbService> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _stores = new();

    public IndexedDbService(ILogger<IndexedDbService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string storeName, string key)
    {
        try
        {
            if (_stores.TryGetValue(storeName, out var store) &&
                store.TryGetValue(key, out var json))
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from transient store: {StoreName}:{Key}", storeName, key);
        }

        return Task.FromResult(default(T?));
    }

    public Task<bool> SetAsync<T>(string storeName, string key, T value)
    {
        try
        {
            var store = _stores.GetOrAdd(storeName, static _ => new ConcurrentDictionary<string, string>());
            var json = JsonSerializer.Serialize(value);
            store[key] = json;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in transient store: {StoreName}:{Key}", storeName, key);
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteAsync(string storeName, string key)
    {
        try
        {
            if (_stores.TryGetValue(storeName, out var store))
            {
                store.TryRemove(key, out _);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item from transient store: {StoreName}:{Key}", storeName, key);
            return Task.FromResult(false);
        }
    }

    public Task<List<string>> GetKeysAsync(string storeName)
    {
        try
        {
            if (_stores.TryGetValue(storeName, out var store))
            {
                return Task.FromResult(store.Keys.ToList());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys from transient store for: {StoreName}", storeName);
        }

        return Task.FromResult(new List<string>());
    }
}
