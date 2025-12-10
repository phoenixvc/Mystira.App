using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for fetching content attribution/creator credits and IP verification
/// </summary>
public interface IAttributionApiClient
{
    /// <summary>
    /// Get attribution information for a scenario
    /// </summary>
    /// <param name="scenarioId">The scenario ID</param>
    /// <returns>Attribution information or null if not found</returns>
    Task<ContentAttribution?> GetScenarioAttributionAsync(string scenarioId);

    /// <summary>
    /// Get attribution information for a content bundle
    /// </summary>
    /// <param name="bundleId">The bundle ID</param>
    /// <returns>Attribution information or null if not found</returns>
    Task<ContentAttribution?> GetBundleAttributionAsync(string bundleId);

    /// <summary>
    /// Get IP registration status for a scenario
    /// </summary>
    /// <param name="scenarioId">The scenario ID</param>
    /// <returns>IP verification status or null if not found</returns>
    Task<IpVerification?> GetScenarioIpStatusAsync(string scenarioId);

    /// <summary>
    /// Get IP registration status for a content bundle
    /// </summary>
    /// <param name="bundleId">The bundle ID</param>
    /// <returns>IP verification status or null if not found</returns>
    Task<IpVerification?> GetBundleIpStatusAsync(string bundleId);
}
