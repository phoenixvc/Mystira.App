namespace Mystira.App.PWA.Services;

/// <summary>
/// Helper class for detecting and handling API connection errors
/// </summary>
public static class ApiConnectionHelper
{
    /// <summary>
    /// Determines if an HttpRequestException indicates the API server is unreachable
    /// </summary>
    public static bool IsConnectionRefused(HttpRequestException ex)
    {
        // In Blazor WebAssembly, connection refused errors typically manifest as:
        // - "TypeError: Failed to fetch" (the most common)
        // - Inner exception message containing "Failed to fetch"
        // - Connection-related errors

        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;

        return message.Contains("failed to fetch")
            || message.Contains("network error")
            || message.Contains("connection refused")
            || innerMessage.Contains("failed to fetch")
            || innerMessage.Contains("connection refused");
    }

    /// <summary>
    /// Gets a user-friendly error message for API connection issues
    /// </summary>
    public static string GetConnectionErrorMessage(string apiBaseUrl, bool isDevelopment)
    {
        if (isDevelopment && IsLocalDevelopmentUrl(apiBaseUrl))
        {
            return $"Unable to connect to the API server at {apiBaseUrl}. " +
                   "Please ensure the API is running. " +
                   "You can start it with: dotnet run --project src/Mystira.App.Api/Mystira.App.Api.csproj";
        }

        return "Unable to connect to the server. Please check your internet connection and try again.";
    }

    /// <summary>
    /// Gets a developer-friendly log message for API connection issues
    /// </summary>
    public static string GetConnectionLogMessage(string apiBaseUrl, bool isDevelopment)
    {
        if (isDevelopment && IsLocalDevelopmentUrl(apiBaseUrl))
        {
            return $"Cannot connect to API at {apiBaseUrl}. The API server may not be running. " +
                   "Start it with: dotnet run --project src/Mystira.App.Api/Mystira.App.Api.csproj";
        }

        return $"Network error connecting to API at {apiBaseUrl}";
    }

    /// <summary>
    /// Determines if a URL points to a local development server
    /// </summary>
    private static bool IsLocalDevelopmentUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // Uri.IsLoopback returns true for localhost, 127.0.0.1, ::1, and other loopback addresses
        return uri.IsLoopback;
    }
}
