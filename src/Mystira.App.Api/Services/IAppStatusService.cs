using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing application status configuration
/// </summary>
public interface IAppStatusService
{
    /// <summary>
    /// Gets the current application status configuration
    /// </summary>
    /// <returns>The current app status configuration</returns>
    Task<AppStatusConfiguration> GetAppStatusAsync();

    /// <summary>
    /// Updates the application status configuration
    /// </summary>
    /// <param name="config">The updated configuration</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateAppStatusAsync(AppStatusConfiguration config);

    /// <summary>
    /// Gets the minimum supported version
    /// </summary>
    /// <returns>The minimum supported version</returns>
    Task<string> GetMinSupportedVersionAsync();

    /// <summary>
    /// Gets the latest version
    /// </summary>
    /// <returns>The latest version</returns>
    Task<string> GetLatestVersionAsync();

    /// <summary>
    /// Gets the current bundle version
    /// </summary>
    /// <returns>The current bundle version</returns>
    Task<string> GetBundleVersionAsync();

    /// <summary>
    /// Updates the bundle version
    /// </summary>
    /// <param name="version">The new bundle version</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateBundleVersionAsync(string version);

    /// <summary>
    /// Checks if maintenance mode is enabled
    /// </summary>
    /// <returns>True if maintenance mode is enabled</returns>
    Task<bool> IsMaintenanceModeAsync();
}
