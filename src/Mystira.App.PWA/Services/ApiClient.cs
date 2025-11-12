using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Scenario>> GetScenariosAsync()
    {
        try
        {
            _logger.LogInformation("Fetching scenarios from API...");
            
            var request = new HttpRequestMessage(HttpMethod.Get, "api/scenarios");
            // This is the key line for CORS
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var scenariosResponse = await response.Content.ReadFromJsonAsync<ScenariosResponse>(_jsonOptions);
                var scenarios = scenariosResponse?.Scenarios ?? new List<Scenario>();
                _logger.LogInformation("Successfully fetched {Count} scenarios", scenarios.Count);
                return scenarios;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode}. No fallback scenarios available.", response.StatusCode);
                return new List<Scenario>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch scenarios: {Message}", ex.Message);
            return new List<Scenario>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scenarios from API. No fallback scenarios available.");
            return new List<Scenario>();
        }
    }

    public async Task<Scenario?> GetScenarioAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching scenario {Id} from API...", id);
            
            var response = await _httpClient.GetAsync($"api/scenarios/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var scenario = await response.Content.ReadFromJsonAsync<Scenario>(_jsonOptions);
                _logger.LogInformation("Successfully fetched scenario {Id}", id);
                return scenario;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode}. Scenario {Id} not available.", response.StatusCode, id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scenario {Id} from API. Scenario not available.", id);
            return null;
        }
    }

    public async Task<Scene?> GetSceneAsync(string scenarioId, string sceneId)
    {
        try
        {
            _logger.LogInformation("Fetching scene '{SceneId}' for scenario {ScenarioId} from API...", sceneId, scenarioId);
            
            var encodedSceneId = Uri.EscapeDataString(sceneId);
            var response = await _httpClient.GetAsync($"api/scenarios/{scenarioId}/scenes/{encodedSceneId}");
            
            if (response.IsSuccessStatusCode)
            {
                var scene = await response.Content.ReadFromJsonAsync<Scene>(_jsonOptions);
                _logger.LogInformation("Successfully fetched scene '{SceneId}'", sceneId);
                return scene;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode}. Scene '{SceneId}' not available.", response.StatusCode, sceneId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scene '{SceneId}' for scenario {ScenarioId} from API. Scene not available.", sceneId, scenarioId);
            return null;
        }
    }

    public async Task<string?> GetMediaUrlFromId(string mediaId)
    {
        try
        {
            _logger.LogInformation("Fetching media id '{MediaId}' from API...", mediaId);
            
            var encodedMediaId = Uri.EscapeDataString(mediaId);
            var response = await _httpClient.GetAsync($"api/Media/{encodedMediaId}/info");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonString);
    
                if (document.RootElement.TryGetProperty("url", out var urlElement) && 
                    urlElement.ValueKind == JsonValueKind.String)
                {
                    var url = urlElement.GetString()!;
                    _logger.LogInformation("Successfully fetched media URL for '{MediaId}'", encodedMediaId);
                    return url;
                }

                _logger.LogWarning("URL property not found in response for '{MediaId}'", encodedMediaId);
                return null;
            }

            _logger.LogWarning("API request failed with status: {StatusCode}. Media '{MediaId}' not available.", 
                response.StatusCode, encodedMediaId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching url for media id {MediaId} from API.", mediaId);
            return null;
        }
    }

    public async Task<PasswordlessSignupResponse?> RequestPasswordlessSignupAsync(string email, string displayName)
    {
        try
        {
            _logger.LogInformation("Requesting passwordless signup for email: {Email}", email);
            
            var request = new { email, displayName };
            var response = await _httpClient.PostAsJsonAsync("api/auth/passwordless/signup", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessSignupResponse>(_jsonOptions);
                _logger.LogInformation("Passwordless signup request successful for: {Email}", email);
                return result;
            }
            else
            {
                _logger.LogWarning("Passwordless signup request failed with status: {StatusCode} for email: {Email}", 
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting passwordless signup for email: {Email}", email);
            return null;
        }
    }

    public async Task<PasswordlessVerifyResponse?> VerifyPasswordlessSignupAsync(string email, string code)
    {
        try
        {
            _logger.LogInformation("Verifying passwordless signup for email: {Email}", email);
            
            var request = new { email, code };
            var response = await _httpClient.PostAsJsonAsync("api/auth/passwordless/verify", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessVerifyResponse>(_jsonOptions);
                _logger.LogInformation("Passwordless signup verification successful for: {Email}", email);
                return result;
            }
            else
            {
                _logger.LogWarning("Passwordless signup verification failed with status: {StatusCode} for email: {Email}", 
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passwordless signup for email: {Email}", email);
            return null;
        }
    }

    public async Task<PasswordlessSigninResponse?> RequestPasswordlessSigninAsync(string email)
    {
        try
        {
            _logger.LogInformation("Requesting passwordless signin for email: {Email}", email);
            
            var request = new { email };
            var response = await _httpClient.PostAsJsonAsync("api/auth/passwordless/signin", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessSigninResponse>(_jsonOptions);
                _logger.LogInformation("Passwordless signin request successful for: {Email}", email);
                return result;
            }
            else
            {
                _logger.LogWarning("Passwordless signin request failed with status: {StatusCode} for email: {Email}", 
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting passwordless signin for email: {Email}", email);
            return null;
        }
    }

    public async Task<PasswordlessVerifyResponse?> VerifyPasswordlessSigninAsync(string email, string code)
    {
        try
        {
            _logger.LogInformation("Verifying passwordless signin for email: {Email}", email);
            
            var request = new { email, code };
            var response = await _httpClient.PostAsJsonAsync("api/auth/passwordless/signin/verify", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessVerifyResponse>(_jsonOptions);
                _logger.LogInformation("Passwordless signin verification successful for: {Email}", email);
                return result;
            }
            else
            {
                _logger.LogWarning("Passwordless signin verification failed with status: {StatusCode} for email: {Email}", 
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying passwordless signin for email: {Email}", email);
            return null;
        }
    }

    public async Task<GameSession?> StartGameSessionAsync(string scenarioId, string accountId, string profileId, List<string> playerNames, string targetAgeGroup)
    {
        try
        {
            _logger.LogInformation("Starting game session for scenario: {ScenarioId}, Account: {AccountId}, Profile: {ProfileId}", 
                scenarioId, accountId, profileId);
            
            var request = new 
            { 
                scenarioId, 
                accountId, 
                profileId, 
                playerNames, 
                targetAgeGroup 
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/gamesessions", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var gameSession = await response.Content.ReadFromJsonAsync<GameSession>(_jsonOptions);
                _logger.LogInformation("Game session started successfully with ID: {SessionId}", gameSession?.Id);
                return gameSession;
            }
            else
            {
                _logger.LogWarning("Failed to start game session with status: {StatusCode} for scenario: {ScenarioId}", 
                    response.StatusCode, scenarioId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game session for scenario: {ScenarioId}", scenarioId);
            return null;
        }
    }
    
}
