using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for fetching content attribution/creator credits
/// </summary>
public class AttributionApiClient : BaseApiClient, IAttributionApiClient
{
    public AttributionApiClient(
        HttpClient httpClient,
        ILogger<AttributionApiClient> logger,
        ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    /// <inheritdoc />
    public async Task<ContentAttribution?> GetScenarioAttributionAsync(string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            Logger.LogWarning("GetScenarioAttributionAsync called with empty scenarioId");
            return null;
        }

        return await SendGetAsync<ContentAttribution>(
            $"api/scenarios/{scenarioId}/attribution",
            $"scenario attribution for {scenarioId}");
    }

    /// <inheritdoc />
    public async Task<ContentAttribution?> GetBundleAttributionAsync(string bundleId)
    {
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            Logger.LogWarning("GetBundleAttributionAsync called with empty bundleId");
            return null;
        }

        return await SendGetAsync<ContentAttribution>(
            $"api/bundles/{bundleId}/attribution",
            $"bundle attribution for {bundleId}");
    }

    /// <inheritdoc />
    public async Task<IpVerification?> GetScenarioIpStatusAsync(string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            Logger.LogWarning("GetScenarioIpStatusAsync called with empty scenarioId");
            return null;
        }

        return await SendGetAsync<IpVerification>(
            $"api/scenarios/{scenarioId}/ip-status",
            $"scenario IP status for {scenarioId}");
    }

    /// <inheritdoc />
    public async Task<IpVerification?> GetBundleIpStatusAsync(string bundleId)
    {
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            Logger.LogWarning("GetBundleIpStatusAsync called with empty bundleId");
            return null;
        }

        return await SendGetAsync<IpVerification>(
            $"api/bundles/{bundleId}/ip-status",
            $"bundle IP status for {bundleId}");
    }
}
