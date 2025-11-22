using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Base class for API clients providing common HTTP functionality
/// </summary>
public abstract class BaseApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly JsonSerializerOptions JsonOptions;
    protected readonly ITokenProvider TokenProvider;

    protected BaseApiClient(HttpClient httpClient, ILogger logger, ITokenProvider tokenProvider)
    {
        HttpClient = httpClient;
        Logger = logger;
        TokenProvider = tokenProvider;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    protected async Task SetAuthorizationHeaderAsync()
    {
        try
        {
            HttpClient.DefaultRequestHeaders.Authorization = null;

            var isAuthenticated = await TokenProvider.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                return;
            }

            var token = await TokenProvider.GetCurrentTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                HttpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error setting authorization header");
        }
    }

    protected async Task<T?> SendGetAsync<T>(
        string endpoint,
        string operationName,
        bool requireAuth = false,
        Action<T?>? onSuccess = null) where T : class
    {
        try
        {
            if (requireAuth)
            {
                await SetAuthorizationHeaderAsync();
            }

            var response = await HttpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                onSuccess?.Invoke(result);
                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogWarning("{OperationName} not found", operationName);
                return null;
            }
            else
            {
                Logger.LogWarning("Failed to fetch {OperationName} with status: {StatusCode}",
                    operationName, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching {OperationName}", operationName);
            return null;
        }
    }

    protected async Task<TResponse?> SendPostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest requestData,
        string operationName,
        bool requireAuth = false,
        Action<TResponse?>? onSuccess = null) where TResponse : class
    {
        try
        {
            if (requireAuth)
            {
                await SetAuthorizationHeaderAsync();
            }

            var response = await HttpClient.PostAsJsonAsync(endpoint, requestData, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
                onSuccess?.Invoke(result);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Failed {OperationName} with status: {StatusCode}. Error: {Error}",
                    operationName, response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during {OperationName}", operationName);
            return null;
        }
    }

    protected async Task<TResponse?> SendPutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest requestData,
        string operationName,
        bool requireAuth = false,
        Action<TResponse?>? onSuccess = null) where TResponse : class
    {
        try
        {
            if (requireAuth)
            {
                await SetAuthorizationHeaderAsync();
            }

            var response = await HttpClient.PutAsJsonAsync(endpoint, requestData, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
                onSuccess?.Invoke(result);
                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogWarning("{OperationName} not found for update", operationName);
                return null;
            }
            else
            {
                Logger.LogWarning("Failed to update {OperationName} with status: {StatusCode}",
                    operationName, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating {OperationName}", operationName);
            return null;
        }
    }

    protected async Task<bool> SendDeleteAsync(
        string endpoint,
        string operationName,
        bool requireAuth = false)
    {
        try
        {
            if (requireAuth)
            {
                await SetAuthorizationHeaderAsync();
            }

            var response = await HttpClient.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Successfully deleted {OperationName}", operationName);
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogWarning("{OperationName} not found for deletion", operationName);
                return false;
            }
            else
            {
                Logger.LogWarning("Failed to delete {OperationName} with status: {StatusCode}",
                    operationName, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting {OperationName}", operationName);
            return false;
        }
    }

    protected string GetApiBaseAddress()
    {
        return HttpClient.BaseAddress!.ToString();
    }

    public string GetApiBaseAddressPublic()
    {
        return GetApiBaseAddress();
    }
}

