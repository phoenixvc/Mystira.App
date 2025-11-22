using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for handling client status and content synchronization operations
/// </summary>
public interface IClientApiService
{
    /// <summary>
    /// Get client status information including version requirements and content updates
    /// </summary>
    /// <param name="clientVersion">Current client version (e.g., 1.3.0)</param>
    /// <param name="contentVersion">Current content bundle version (e.g., 2025-05-28)</param>
    /// <returns>Status information with version requirements and content updates</returns>
    Task<ClientStatusResponse> GetClientStatusAsync(string clientVersion, string contentVersion);

    /// <summary>
    /// Build a content manifest with changes since the client's content version
    /// </summary>
    /// <param name="clientContentVersion">The client's current content version</param>
    /// <returns>A content manifest with added, updated, and removed items</returns>
    Task<ContentManifest> BuildContentManifestAsync(string clientContentVersion);

    /// <summary>
    /// Check if a client version is below the minimum supported version
    /// </summary>
    /// <param name="clientVersion">The client version to check</param>
    /// <returns>True if an update is required, false otherwise</returns>
    Task<bool> IsUpdateRequiredAsync(string clientVersion);

    /// <summary>
    /// Get the current bundle version for content
    /// </summary>
    /// <returns>The current content bundle version</returns>
    Task<string> GetCurrentBundleVersionAsync();

    /// <summary>
    /// Get version information including minimum supported and latest versions
    /// </summary>
    /// <returns>Tuple with minimum supported and latest versions</returns>
    Task<(string MinSupportedVersion, string LatestVersion)> GetVersionInfoAsync();
}
