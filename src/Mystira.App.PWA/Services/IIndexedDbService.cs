namespace Mystira.App.PWA.Services;

public interface IIndexedDbService
{
    Task<T?> GetAsync<T>(string storeName, string key);
    Task<bool> SetAsync<T>(string storeName, string key, T value);
    Task<bool> DeleteAsync(string storeName, string key);
    Task<List<string>> GetKeysAsync(string storeName);
}
