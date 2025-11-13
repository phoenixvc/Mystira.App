using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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

    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Fetching account for email: {Email}", email);
            
            var encodedEmail = Uri.EscapeDataString(email);
            var response = await _httpClient.GetAsync($"api/accounts/email/{encodedEmail}");
            
            if (response.IsSuccessStatusCode)
            {
                var account = await response.Content.ReadFromJsonAsync<Account>(_jsonOptions);
                _logger.LogInformation("Successfully fetched account for email: {Email}", email);
                return account;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Account not found for email: {Email}", email);
                return null;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode} for email: {Email}", 
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching account for email: {Email}", email);
            return null;
        }
    }

    public async Task<GameSession?> EndGameSessionAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("Ending game session: {SessionId}", sessionId);
            
            var response = await _httpClient.PostAsync($"api/gamesessions/{sessionId}/end", null);
            
            if (response.IsSuccessStatusCode)
            {
                var gameSession = await response.Content.ReadFromJsonAsync<GameSession>(_jsonOptions);
                _logger.LogInformation("Game session ended successfully: {SessionId}", sessionId);
                return gameSession;
            }
            else
            {
                _logger.LogWarning("Failed to end game session with status: {StatusCode} for session: {SessionId}", 
                    response.StatusCode, sessionId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending game session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<List<GameSession>?> GetSessionsByAccountAsync(string accountId)
    {
        try
        {
            _logger.LogInformation("Fetching sessions for account: {AccountId}", accountId);
            
            var response = await _httpClient.GetAsync($"api/gamesessions/account/{accountId}");
            
            if (response.IsSuccessStatusCode)
            {
                var sessions = await response.Content.ReadFromJsonAsync<List<GameSession>>(_jsonOptions);
                _logger.LogInformation("Successfully fetched {Count} sessions for account: {AccountId}", 
                    sessions?.Count ?? 0, accountId);
                return sessions;
            }
            else
            {
                _logger.LogWarning("Failed to fetch sessions with status: {StatusCode} for account: {AccountId}", 
                    response.StatusCode, accountId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sessions for account: {AccountId}", accountId);
            return null;
        }
    }
    
    public async Task<Character?> GetCharacterAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching character {Id} from API...", id);
            
            var response = await _httpClient.GetAsync($"api/character/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var character = await response.Content.ReadFromJsonAsync<Character>(_jsonOptions);
                _logger.LogInformation("Successfully fetched character {Id}", id);
                return character;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode}. Character {Id} not available.", response.StatusCode, id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching character {Id} from API.", id);
            return null;
        }
    }

    public async Task<List<Character>?> GetCharactersAsync()
    {
        try
        {
            _logger.LogInformation("Fetching characters from API...");
            
            var response = await _httpClient.GetAsync("api/charactermaps");
            
            if (response.IsSuccessStatusCode)
            {
                var characters = await response.Content.ReadFromJsonAsync<List<Character>>(_jsonOptions);
                _logger.LogInformation("Successfully fetched {Count} characters", characters?.Count ?? 0);
                return characters;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode}. No characters available.", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching characters from API.");
            return null;
        }
    }

    public async Task<UserProfile?> GetProfileAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching profile {Id} from API...", id);
            
            var response = await _httpClient.GetAsync($"api/userprofiles/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                _logger.LogInformation("Successfully fetched profile {Id}", id);
                return profile;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Profile not found: {Id}", id);
                return null;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode} for profile: {Id}", response.StatusCode, id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile {Id} from API.", id);
            return null;
        }
    }

    public async Task<UserProfile?> GetProfileByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching profile by ID {Id} from API...", id);
            
            var response = await _httpClient.GetAsync($"api/userprofiles/id/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                _logger.LogInformation("Successfully fetched profile by ID {Id}", id);
                return profile;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Profile not found by ID: {Id}", id);
                return null;
            }
            else
            {
                _logger.LogWarning("API request failed with status: {StatusCode} for profile ID: {Id}", response.StatusCode, id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile by ID {Id} from API.", id);
            return null;
        }
    }

    public async Task<List<UserProfile>?> GetProfilesByAccountAsync(string accountId)
    {
        try
        {
            _logger.LogInformation("Fetching profiles for account {AccountId} from API...", accountId);
            
            var response = await _httpClient.GetAsync($"api/userprofiles/account/{accountId}");
            
            if (response.IsSuccessStatusCode)
            {
                var profiles = await response.Content.ReadFromJsonAsync<List<UserProfile>>(_jsonOptions);
                _logger.LogInformation("Successfully fetched {Count} profiles for account {AccountId}", profiles?.Count ?? 0, accountId);
                return profiles;
            }
            else
            {
                _logger.LogWarning("Failed to fetch profiles with status: {StatusCode} for account: {AccountId}", response.StatusCode, accountId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profiles for account {AccountId} from API.", accountId);
            return null;
        }
    }

    public async Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request)
    {
        try
        {
            _logger.LogInformation("Creating profile {Name} via API...", request.Name);
            
            var response = await _httpClient.PostAsJsonAsync("api/userprofiles", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                _logger.LogInformation("Successfully created profile {Name} with ID {Id}", request.Name, profile?.Id);
                return profile;
            }
            else
            {
                _logger.LogWarning("Failed to create profile with status: {StatusCode} for name: {Name}", response.StatusCode, request.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile {Name} via API.", request.Name);
            return null;
        }
    }

    public async Task<List<UserProfile>?> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request)
    {
        try
        {
            _logger.LogInformation("Creating {Count} profiles via API...", request.Profiles.Count);
            
            var response = await _httpClient.PostAsJsonAsync("api/userprofiles/batch", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var profiles = await response.Content.ReadFromJsonAsync<List<UserProfile>>(_jsonOptions);
                _logger.LogInformation("Successfully created {Count} profiles", profiles?.Count ?? 0);
                return profiles;
            }
            else
            {
                _logger.LogWarning("Failed to create multiple profiles with status: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple profiles via API.");
            return null;
        }
    }

    public async Task<UserProfile?> UpdateProfileAsync(string id, UpdateUserProfileRequest request)
    {
        try
        {
            _logger.LogInformation("Updating profile {Id} via API...", id);
            
            var response = await _httpClient.PutAsJsonAsync($"api/userprofiles/{id}", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                _logger.LogInformation("Successfully updated profile {Id}", id);
                return profile;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Profile not found for update: {Id}", id);
                return null;
            }
            else
            {
                _logger.LogWarning("Failed to update profile with status: {StatusCode} for ID: {Id}", response.StatusCode, id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {Id} via API.", id);
            return null;
        }
    }

    public async Task<bool> DeleteProfileAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting profile {Id} via API...", id);
            
            var response = await _httpClient.DeleteAsync($"api/userprofiles/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted profile {Id}", id);
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Profile not found for deletion: {Id}", id);
                return false;
            }
            else
            {
                _logger.LogWarning("Failed to delete profile with status: {StatusCode} for ID: {Id}", response.StatusCode, id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {Id} via API.", id);
            return false;
        }
    }

    public string GetApiBaseAddress()
    {
        return _httpClient.BaseAddress!.ToString();
    }

    public string GetMediaResourceEndpointUrl(string mediaId)
    {
        return $"{GetApiBaseAddress()}api/media/{mediaId}";
    }
}
