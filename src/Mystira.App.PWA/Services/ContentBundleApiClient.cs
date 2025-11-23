using System.Net.Http.Json;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for content bundle-related operations
/// </summary>
public class ContentBundleApiClient : BaseApiClient, IContentBundleApiClient
{
    public ContentBundleApiClient(HttpClient httpClient, ILogger<ContentBundleApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<List<ContentBundle>> GetBundlesAsync()
    {
        try
        {
            // Bundles endpoint is public - no auth required
            var response = await HttpClient.GetAsync("api/bundles");
            if (response.IsSuccessStatusCode)
            {
                var bundles = await response.Content.ReadFromJsonAsync<List<ContentBundle>>(JsonOptions);
                return bundles ?? new List<ContentBundle>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching bundles");
        }
        return new List<ContentBundle>();
    }

    public async Task<List<ContentBundle>> GetBundlesByAgeGroupAsync(string ageGroup)
    {
        try
        {
            // Bundles endpoint is public - no auth required
            var response = await HttpClient.GetAsync($"api/bundles/age-group/{ageGroup}");
            if (response.IsSuccessStatusCode)
            {
                var bundles = await response.Content.ReadFromJsonAsync<List<ContentBundle>>(JsonOptions);
                return bundles ?? new List<ContentBundle>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching bundles by age group {AgeGroup}", ageGroup);
        }
        return new List<ContentBundle>();
    }
}

