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
                    var dto = JsonSerializer.Deserialize<ScenarioListResponseDto>(content, JsonOptions);
                    if (dto?.Scenarios != null)
                    {
                        var mapped = dto.Scenarios.Select(s => new Scenario
                        {
                            Id = s.Id,
                            Title = s.Title,
                            Image = s.Image,
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
                                        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var i))
                                        {
                                            return i;
                                        }

                                        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var si))
                                        {
                                            return si;
                                        }
                                    }
                                    return fallback;
                                }

                                DateTime GetDate(string name)
                                {
                                    if (item.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                                    {
                                        if (DateTime.TryParse(prop.GetString(), out var dt))
                                        {
                                            return dt;
                                        }
                                    }
                                    return DateTime.UtcNow;
                                }

                                string[] GetStringArray(string name)
                                {
                                    if (!item.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
                                    {
                                        return Array.Empty<string>();
                                    }

                                    var list = new List<string>();
                                    foreach (var el in prop.EnumerateArray())
                                    {
                                        if (el.ValueKind == JsonValueKind.String)
                                        {
                                            var v = el.GetString();
                                            if (!string.IsNullOrWhiteSpace(v))
                                            {
                                                list.Add(v!);
                                            }
                                        }
                                        else if (el.ValueKind == JsonValueKind.Object)
                                        {
                                            if (el.TryGetProperty("value", out var vProp) && vProp.ValueKind == JsonValueKind.String)
                                            {
                                                var v = vProp.GetString();
                                                if (!string.IsNullOrWhiteSpace(v))
                                                {
                                                    list.Add(v!);
                                                }
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
                                    Image = GetString("image"),
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
            catch (JsonException jsonEx)
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
        try
        {
            var url = $"api/scenarios/{id}";
            Logger.LogInformation("Fetching scenario {Id} from API: {Url}", id, url);

            var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.LogWarning("Scenario not found: {Id}", id);
                    return null;
                }

                Logger.LogWarning("API request failed with status: {StatusCode} for scenario {Id}", response.StatusCode, id);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                Logger.LogWarning("Scenario {Id} response was empty", id);
                return null;
            }

            var trimmed = content.TrimStart();
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    Logger.LogWarning("Unexpected scenario payload shape for {Id}: {Kind}", id, doc.RootElement.ValueKind);
                    return null;
                }

                var root = doc.RootElement;

                string GetString(JsonElement obj, string name)
                {
                    return obj.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
                        ? (prop.GetString() ?? string.Empty)
                        : string.Empty;
                }

                int GetInt(JsonElement obj, string name, int fallback = 0)
                {
                    if (obj.TryGetProperty(name, out var prop))
                    {
                        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var i))
                        {
                            return i;
                        }

                        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var si))
                        {
                            return si;
                        }
                    }
                    return fallback;
                }

                DateTime GetDate(JsonElement obj, string name)
                {
                    if (obj.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(prop.GetString(), out var dt))
                        {
                            return dt;
                        }
                    }
                    return DateTime.UtcNow;
                }

                string[] GetStringArray(JsonElement obj, string name)
                {
                    if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
                    {
                        return Array.Empty<string>();
                    }

                    var list = new List<string>();
                    foreach (var el in prop.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            var v = el.GetString();
                            if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, "null", StringComparison.OrdinalIgnoreCase))
                            {
                                list.Add(v!);
                            }
                        }
                        else if (el.ValueKind == JsonValueKind.Object)
                        {
                            if (el.TryGetProperty("value", out var vProp) && vProp.ValueKind == JsonValueKind.String)
                            {
                                var v = vProp.GetString();
                                if (!string.IsNullOrWhiteSpace(v))
                                {
                                    list.Add(v!);
                                }
                            }
                        }
                    }
                    return list.ToArray();
                }

                SceneMedia? ReadMedia(JsonElement obj)
                {
                    if (!obj.TryGetProperty("media", out var media) || media.ValueKind != JsonValueKind.Object)
                    {
                        return null;
                    }

                    string? Clean(string s) => string.IsNullOrWhiteSpace(s) || string.Equals(s, "null", StringComparison.OrdinalIgnoreCase) ? null : s;
                    return new SceneMedia
                    {
                        Image = Clean(GetString(media, "image")),
                        Audio = Clean(GetString(media, "audio")),
                        Video = Clean(GetString(media, "video"))
                    };
                }

                List<SceneBranch> ReadBranches(JsonElement obj)
                {
                    var branches = new List<SceneBranch>();
                    if (!obj.TryGetProperty("branches", out var arr) || arr.ValueKind != JsonValueKind.Array)
                    {
                        return branches;
                    }

                    foreach (var br in arr.EnumerateArray().Where(br => br.ValueKind == JsonValueKind.Object))
                    {
                        string? compassAxis = null;
                        string? compassDirection = null;
                        double? compassDelta = null;

                        if (br.TryGetProperty("compassChange", out var compassChange) && compassChange.ValueKind == JsonValueKind.Object)
                        {
                            compassAxis = GetString(compassChange, "axis");
                            if (compassChange.TryGetProperty("delta", out var deltaProp))
                            {
                                if (deltaProp.ValueKind == JsonValueKind.Number && deltaProp.TryGetDouble(out var deltaValue))
                                {
                                    compassDirection = deltaValue < 0 ? "negative" : "positive";
                                    compassDelta = deltaValue;
                                }
                                else if (deltaProp.ValueKind == JsonValueKind.String && double.TryParse(deltaProp.GetString(), out var deltaString))
                                {
                                    compassDirection = deltaString < 0 ? "negative" : "positive";
                                    compassDelta = deltaString;
                                }
                            }
                        }

                        branches.Add(new SceneBranch
                        {
                            Choice = GetString(br, "choice"),
                            NextSceneId = GetString(br, "nextSceneId"),
                            CompassAxis = !string.IsNullOrWhiteSpace(compassAxis) ? compassAxis : null,
                            CompassDirection = compassDirection,
                            CompassDelta = compassDelta
                        });
                    }

                    return branches;
                }

                List<Scene> ReadScenes(JsonElement obj)
                {
                    var scenes = new List<Scene>();
                    if (!obj.TryGetProperty("scenes", out var arr) || arr.ValueKind != JsonValueKind.Array)
                    {
                        return scenes;
                    }

                    foreach (var s in arr.EnumerateArray())
                    {
                        if (s.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var type = GetString(s, "type");
                        // activeCharacter can be provided as camelCase or snake_case depending on the source
                        var activeChar = GetString(s, "activeCharacter");
                        if (string.IsNullOrWhiteSpace(activeChar) && s.TryGetProperty("active_character", out var acProp) && acProp.ValueKind == JsonValueKind.String)
                        {
                            activeChar = acProp.GetString() ?? string.Empty;
                        }

                        var scene = new Scene
                        {
                            Id = GetString(s, "id"),
                            Title = GetString(s, "title"),
                            Description = GetString(s, "description"),
                            Type = type,
                            NextSceneId = GetString(s, "nextSceneId"),
                            Media = ReadMedia(s),
                            Branches = ReadBranches(s),
                            Difficulty = s.TryGetProperty("difficulty", out var diff) && diff.ValueKind == JsonValueKind.Number && diff.TryGetInt32(out var d)
                                ? d
                                : (int?)null,
                            ActiveCharacter = string.IsNullOrWhiteSpace(activeChar) ? null : activeChar
                        };
                        scenes.Add(scene);
                    }
                    return scenes;
                }

                List<ScenarioCharacter> ReadCharacters(JsonElement obj)
                {
                    var chars = new List<ScenarioCharacter>();
                    if (!obj.TryGetProperty("characters", out var arr) || arr.ValueKind != JsonValueKind.Array)
                    {
                        return chars;
                    }

                    foreach (var c in arr.EnumerateArray())
                    {
                        if (c.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var ch = new ScenarioCharacter
                        {
                            Id = GetString(c, "id"),
                            Name = GetString(c, "name"),
                            Image = !string.Equals(GetString(c, "image"), "null", StringComparison.OrdinalIgnoreCase) ? GetString(c, "image") : null,
                            Audio = !string.Equals(GetString(c, "audio"), "null", StringComparison.OrdinalIgnoreCase) ? GetString(c, "audio") : null,
                            Metadata = new ScenarioCharacterMetadata()
                        };

                        if (c.TryGetProperty("metadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
                        {
                            ch.Metadata.Role = GetStringArray(meta, "role").ToList();
                            ch.Metadata.Archetype = GetStringArray(meta, "archetype").ToList();
                            ch.Metadata.Species = GetString(meta, "species");
                            ch.Metadata.Traits = GetStringArray(meta, "traits").ToList();
                            ch.Metadata.Backstory = GetString(meta, "backstory");

                            // age can be number or string
                            ch.Metadata.Age = GetInt(meta, "age", 0);
                        }

                        chars.Add(ch);
                    }
                    return chars;
                }

                var scenario = new Scenario
                {
                    Id = GetString(root, "id"),
                    Title = GetString(root, "title"),
                    Description = GetString(root, "description"),
                    Tags = GetStringArray(root, "tags"),
                    Difficulty = GetString(root, "difficulty"),
                    SessionLength = GetString(root, "sessionLength"),
                    Archetypes = GetStringArray(root, "archetypes"),
                    MinimumAge = GetInt(root, "minimumAge", 1),
                    AgeGroup = GetString(root, "ageGroup"),
                    CoreAxes = GetStringArray(root, "coreAxes").ToList(),
                    CreatedAt = GetDate(root, "createdAt"),
                    Scenes = ReadScenes(root),
                    Characters = ReadCharacters(root)
                };

                if (string.IsNullOrWhiteSpace(scenario.Id))
                {
                    Logger.LogWarning("Parsed scenario payload for {Id} but missing Id field", id);
                    return null;
                }

                Logger.LogInformation("Fetched scenario {Id}: {Title} with {SceneCount} scenes and {CharCount} characters",
                    scenario.Id, scenario.Title, scenario.Scenes.Count, scenario.Characters.Count);
                return scenario;
            }
            catch (JsonException jx)
            {
                var preview = trimmed.Length > 256 ? trimmed.Substring(0, 256) + "..." : trimmed;
                Logger.LogError(jx, "Failed to parse scenario {Id}. Preview: {Preview}", id, preview);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching scenario {Id}", id);
            return null;
        }
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

