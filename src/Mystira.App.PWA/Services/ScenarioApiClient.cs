using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for scenario-related operations
/// </summary>
public class ScenarioApiClient : BaseApiClient, IScenarioApiClient
{
    public ScenarioApiClient(HttpClient httpClient, ILogger<ScenarioApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<List<Scenario>> GetScenariosAsync()
    {
        try
        {
            Logger.LogInformation("Fetching scenarios from API...");

            // Scenarios endpoint is public - no auth required
            var request = new HttpRequestMessage(HttpMethod.Get, "api/scenarios");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await HttpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Try to parse as ScenariosResponse first (object with Scenarios property)
                try
                {
                    var scenariosResponse = await response.Content.ReadFromJsonAsync<ScenariosResponse>(JsonOptions);
                    var scenarios = scenariosResponse?.Scenarios ?? new List<Scenario>();
                    Logger.LogInformation("Successfully fetched {Count} scenarios (ScenariosResponse format)", scenarios.Count);
                    return scenarios;
                }
                catch (System.Text.Json.JsonException)
                {
                    // If that fails, try to parse as a direct array of scenarios
                    Logger.LogInformation("Attempting to parse response as direct scenario array...");

                    // Re-fetch because the stream was consumed
                    using (var retryRequest = new HttpRequestMessage(HttpMethod.Get, "api/scenarios"))
                    {
                        retryRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
                        var retryResponse = await HttpClient.SendAsync(retryRequest);

                    if (retryResponse.IsSuccessStatusCode)
                    {
                        var scenarios = await retryResponse.Content.ReadFromJsonAsync<List<Scenario>>(JsonOptions);
                        Logger.LogInformation("Successfully fetched {Count} scenarios (array format)", scenarios?.Count ?? 0);
                        return scenarios ?? new List<Scenario>();
                        }
                    }
                }

                return new List<Scenario>();
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode}. No fallback scenarios available.", response.StatusCode);
                return new List<Scenario>();
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to fetch scenarios: {Message}", ex.Message);
            return new List<Scenario>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching scenarios from API. No fallback scenarios available.");
            return new List<Scenario>();
        }
    }

    public async Task<Scenario?> GetScenarioAsync(string id)
    {
        return await SendGetAsync<Scenario>(
            $"api/scenarios/{id}",
            $"scenario {id}",
            requireAuth: false,
            onSuccess: result => Logger.LogInformation("Successfully fetched scenario {Id}", id));
    }

    public async Task<Scene?> GetSceneAsync(string scenarioId, string sceneId)
    {
        try
        {
            Logger.LogInformation("Fetching scene '{SceneId}' for scenario {ScenarioId} from API...", sceneId, scenarioId);

            var encodedSceneId = Uri.EscapeDataString(sceneId);
            var response = await HttpClient.GetAsync($"api/scenarios/{scenarioId}/scenes/{encodedSceneId}");

            if (response.IsSuccessStatusCode)
            {
                var scene = await response.Content.ReadFromJsonAsync<Scene>(JsonOptions);
                Logger.LogInformation("Successfully fetched scene '{SceneId}'", sceneId);
                return scene;
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode}. Scene '{SceneId}' not available.", response.StatusCode, sceneId);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching scene '{SceneId}' for scenario {ScenarioId} from API. Scene not available.", sceneId, scenarioId);
            return null;
        }
    }

    public async Task<ScenarioGameStateResponse?> GetScenariosWithGameStateAsync(string accountId)
    {
        try
        {
            Logger.LogInformation("Fetching scenarios with game state for account: {AccountId}", accountId);

            var response = await HttpClient.GetAsync($"api/scenarios/with-game-state/{accountId}");

            if (response.IsSuccessStatusCode)
            {
                var gameStateResponse = await response.Content.ReadFromJsonAsync<ScenarioGameStateResponse>(JsonOptions);
                Logger.LogInformation("Successfully fetched game state for {Count} scenarios", gameStateResponse?.TotalCount ?? 0);
                return gameStateResponse;
            }
            else
            {
                Logger.LogWarning("Failed to fetch scenarios with game state with status: {StatusCode} for account: {AccountId}",
                    response.StatusCode, accountId);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching scenarios with game state for account: {AccountId}", accountId);
            return null;
        }
    }

    public async Task<bool> CompleteScenarioForAccountAsync(string accountId, string scenarioId)
    {
        try
        {
            Logger.LogInformation("Marking scenario {ScenarioId} as complete for account {AccountId}", scenarioId, accountId);

            var request = new CompleteScenarioRequest
            {
                AccountId = accountId,
                ScenarioId = scenarioId
            };

            var response = await HttpClient.PostAsJsonAsync("api/gamesessions/complete-scenario", request);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Successfully marked scenario {ScenarioId} as complete for account {AccountId}",
                    scenarioId, accountId);
                return true;
            }
            else
            {
                Logger.LogWarning("Failed to complete scenario with status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error completing scenario {ScenarioId} for account {AccountId}", scenarioId, accountId);
            return false;
        }
    }
}

