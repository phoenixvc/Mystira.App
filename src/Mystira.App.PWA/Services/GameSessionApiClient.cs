using System.Net.Http.Json;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for game session-related operations
/// </summary>
public class GameSessionApiClient : BaseApiClient, IGameSessionApiClient
{
    public GameSessionApiClient(HttpClient httpClient, ILogger<GameSessionApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<GameSession?> StartGameSessionAsync(string scenarioId, string accountId, string profileId, List<string> playerNames, string targetAgeGroup)
    {
        try
        {
            Logger.LogInformation("Starting game session for scenario: {ScenarioId}, Account: {AccountId}, Profile: {ProfileId}",
                scenarioId, accountId, profileId);

            var requestData = new
            {
                scenarioId,
                accountId,
                profileId,
                playerNames,
                targetAgeGroup
            };

            var response = await HttpClient.PostAsJsonAsync("api/gamesessions", requestData, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var gameSession = await response.Content.ReadFromJsonAsync<GameSession>(JsonOptions);
                Logger.LogInformation("Game session started successfully with ID: {SessionId}", gameSession?.Id);
                return gameSession;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Failed to start game session with status: {StatusCode} for scenario: {ScenarioId}. Error: {Error}",
                    response.StatusCode, scenarioId, errorContent);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error starting game session for scenario: {ScenarioId}", scenarioId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Request timed out starting game session for scenario: {ScenarioId}", scenarioId);
            return null;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Error parsing API response when starting game session for scenario: {ScenarioId}", scenarioId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error starting game session for scenario: {ScenarioId}", scenarioId);
            return null;
        }
    }

    public async Task<GameSession?> EndGameSessionAsync(string sessionId)
    {
        try
        {
            Logger.LogInformation("Ending game session: {SessionId}", sessionId);

            var response = await HttpClient.PostAsync($"api/gamesessions/{sessionId}/end", null);

            if (response.IsSuccessStatusCode)
            {
                var gameSession = await response.Content.ReadFromJsonAsync<GameSession>(JsonOptions);
                Logger.LogInformation("Game session ended successfully: {SessionId}", sessionId);
                return gameSession;
            }
            else
            {
                Logger.LogWarning("Failed to end game session with status: {StatusCode} for session: {SessionId}",
                    response.StatusCode, sessionId);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error ending game session: {SessionId}", sessionId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Request timed out ending game session: {SessionId}", sessionId);
            return null;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Error parsing API response when ending game session: {SessionId}", sessionId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error ending game session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<GameSession?> ProgressSessionSceneAsync(string sessionId, string sceneId)
    {
        try
        {
            Logger.LogInformation("Progressing session {SessionId} to scene {SceneId}", sessionId, sceneId);

            var requestData = new { sceneId };
            var response = await HttpClient.PostAsJsonAsync($"api/gamesessions/{sessionId}/progress-scene", requestData, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var gameSession = await response.Content.ReadFromJsonAsync<GameSession>(JsonOptions);
                Logger.LogInformation("Game session progressed successfully: {SessionId} to scene {SceneId}", sessionId, sceneId);
                return gameSession;
            }
            else
            {
                Logger.LogWarning("Failed to progress session with status: {StatusCode} for session: {SessionId}, scene: {SceneId}",
                    response.StatusCode, sessionId, sceneId);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error progressing session {SessionId} to scene {SceneId}", sessionId, sceneId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Request timed out progressing session {SessionId} to scene {SceneId}", sessionId, sceneId);
            return null;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Error parsing API response when progressing session {SessionId} to scene {SceneId}", sessionId, sceneId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error progressing session {SessionId} to scene {SceneId}", sessionId, sceneId);
            return null;
        }
    }

    public async Task<List<GameSession>?> GetSessionsByAccountAsync(string accountId)
    {
        try
        {
            Logger.LogInformation("Fetching sessions for account: {AccountId}", accountId);

            var response = await HttpClient.GetAsync($"api/gamesessions/account/{accountId}");

            if (response.IsSuccessStatusCode)
            {
                var sessions = await response.Content.ReadFromJsonAsync<List<GameSession>>(JsonOptions);
                Logger.LogInformation("Successfully fetched {Count} sessions for account: {AccountId}",
                    sessions?.Count ?? 0, accountId);
                return sessions;
            }
            else
            {
                Logger.LogWarning("Failed to fetch sessions with status: {StatusCode} for account: {AccountId}",
                    response.StatusCode, accountId);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error fetching sessions for account: {AccountId}", accountId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Request timed out fetching sessions for account: {AccountId}", accountId);
            return null;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Error parsing API response when progressing session {SessionId} to scene {SceneId}", sessionId, sceneId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing API response when fetching sessions for account: {AccountId}", accountId);
            return null;
        }
    }

    public async Task<List<GameSession>?> GetInProgressSessionsAsync(string accountId)
    {
        try
        {
            Logger.LogInformation("Fetching in-progress sessions for account: {AccountId}", accountId);

            var response = await HttpClient.GetAsync($"api/gamesessions/account/{accountId}/in-progress");

            if (response.IsSuccessStatusCode)
            {
                var sessions = await response.Content.ReadFromJsonAsync<List<GameSession>>(JsonOptions);
                Logger.LogInformation("Successfully fetched {Count} in-progress sessions for account: {AccountId}",
                    sessions?.Count ?? 0, accountId);
                return sessions;
            }
            else
            {
                Logger.LogWarning("Failed to fetch in-progress sessions with status: {StatusCode} for account: {AccountId}",
                    response.StatusCode, accountId);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error fetching in-progress sessions for account: {AccountId}", accountId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Request timed out fetching in-progress sessions for account: {AccountId}", accountId);
            return null;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Error parsing API response when fetching in-progress sessions for account: {AccountId}", accountId);
            return null;
        }
    }
}

