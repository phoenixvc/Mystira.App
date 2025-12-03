using System.Net.Http.Json;
using System.Text.Json;
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
            // Scenarios endpoint is public - no auth required
            var url = "api/scenarios?page=1&pageSize=100";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            Logger.LogInformation("Fetching scenarios from API: {Url}", url);

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("API request failed with status: {StatusCode}", response.StatusCode);
                return new List<Scenario>();
            }

            // Read content ONCE and sniff the JSON shape to avoid stream reuse and JsonException surfacing
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                Logger.LogWarning("Scenarios response was empty");
                return new List<Scenario>();
            }

            var trimmed = content.TrimStart();
            var first = trimmed[0];

            try
            {
                if (first == '{')
                {
                    // Paginated response: Mystira.App.Contracts.Responses.Scenarios.ScenarioListResponse
                    var dto = System.Text.Json.JsonSerializer.Deserialize<ScenarioListResponseDto>(content, JsonOptions);
                    if (dto?.Scenarios != null)
                    {
                        var mapped = dto.Scenarios.Select(s => new Scenario
                        {
                            Id = s.Id,
                            Title = s.Title,
                            Description = s.Description,
                            Tags = s.Tags?.ToArray() ?? Array.Empty<string>(),
                            Difficulty = s.Difficulty ?? string.Empty,
                            SessionLength = s.SessionLength ?? string.Empty,
                            Archetypes = s.Archetypes?.ToArray() ?? Array.Empty<string>(),
                            MinimumAge = s.MinimumAge,
                            AgeGroup = s.AgeGroup ?? string.Empty,
                            CoreAxes = s.CoreAxes ?? new List<string>(),
                            CreatedAt = s.CreatedAt,
                            Scenes = new List<Scene>()
                        }).ToList();

                        Logger.LogInformation("Fetched {Count} scenarios (paginated)", mapped.Count);
                        return mapped;
                    }
                }
                else if (first == '[')
                {
                    // Raw array of full scenarios (admin/export shape). Normalize to PWA Scenario model.
                    // Some collections (e.g., archetypes, coreAxes) may be arrays of objects with a 'value' property
                    // rather than plain strings; handle both.
                    var normalized = new List<Scenario>();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            try
                            {
                                string GetString(string name)
                                {
                                    return item.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
                                        ? prop.GetString() ?? string.Empty
                                        : string.Empty;
                                }

                                int GetInt(string name, int fallback = 0)
                                {
                                    if (item.TryGetProperty(name, out var prop))
                                    {
                                        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var i)) return i;
                                        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var si)) return si;
                                    }
                                    return fallback;
                                }

                                DateTime GetDate(string name)
                                {
                                    if (item.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                                    {
                                        if (DateTime.TryParse(prop.GetString(), out var dt)) return dt;
                                    }
                                    return DateTime.UtcNow;
                                }

                                string[] GetStringArray(string name)
                                {
                                    if (!item.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
                                        return Array.Empty<string>();

                                    var list = new List<string>();
                                    foreach (var el in prop.EnumerateArray())
                                    {
                                        if (el.ValueKind == JsonValueKind.String)
                                        {
                                            var v = el.GetString();
                                            if (!string.IsNullOrWhiteSpace(v)) list.Add(v!);
                                        }
                                        else if (el.ValueKind == JsonValueKind.Object)
                                        {
                                            if (el.TryGetProperty("value", out var vProp) && vProp.ValueKind == JsonValueKind.String)
                                            {
                                                var v = vProp.GetString();
                                                if (!string.IsNullOrWhiteSpace(v)) list.Add(v!);
                                            }
                                        }
                                    }
                                    return list.ToArray();
                                }

                                var scenario = new Scenario
                                {
                                    Id = GetString("id"),
                                    Title = GetString("title"),
                                    Description = GetString("description"),
                                    Tags = GetStringArray("tags"),
                                    Difficulty = GetString("difficulty"),
                                    SessionLength = GetString("sessionLength"),
                                    Archetypes = GetStringArray("archetypes"),
                                    MinimumAge = GetInt("minimumAge", 1),
                                    AgeGroup = GetString("ageGroup"),
                                    CoreAxes = GetStringArray("coreAxes").ToList(),
                                    CreatedAt = GetDate("createdAt"),
                                    Scenes = new List<Scene>()
                                };

                                // Only add items with an Id and Title to avoid empty shells
                                if (!string.IsNullOrWhiteSpace(scenario.Id))
                                {
                                    normalized.Add(scenario);
                                }
                            }
                            catch (Exception perItemEx)
                            {
                                Logger.LogWarning(perItemEx, "Skipping malformed scenario item in array payload");
                            }
                        }
                    }

                    Logger.LogInformation("Fetched {Count} scenarios (array)", normalized.Count);
                    return normalized;
                }
                else
                {
                    Logger.LogWarning("Unrecognized scenarios payload start: '{FirstChar}'. Returning empty list.", first);
                    return new List<Scenario>();
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                var preview = trimmed.Length > 256 ? trimmed.Substring(0, 256) + "..." : trimmed;
                Logger.LogWarning(jsonEx, "Failed to parse scenarios payload. Preview: {Preview}", preview);
                return new List<Scenario>();
            }

            // If we reach here, the payload didn't yield any scenarios
            Logger.LogWarning("Scenarios payload parsed but contained no items.");
            return new List<Scenario>();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to fetch scenarios: {Message}", ex.Message);
            return new List<Scenario>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching scenarios from API.");
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

