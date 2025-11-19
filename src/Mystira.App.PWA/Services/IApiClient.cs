using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IApiClient
{
    Task<List<Scenario>> GetScenariosAsync();
    Task<Scenario?> GetScenarioAsync(string id);
    Task<Scene?> GetSceneAsync(string scenarioId, string sceneId);
    Task<string?> GetMediaUrlFromId(string mediaId);
    Task<PasswordlessSignupResponse?> RequestPasswordlessSignupAsync(string email, string displayName);
    Task<PasswordlessVerifyResponse?> VerifyPasswordlessSignupAsync(string email, string code);
    Task<PasswordlessSigninResponse?> RequestPasswordlessSigninAsync(string email);
    Task<PasswordlessVerifyResponse?> VerifyPasswordlessSigninAsync(string email, string code);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string token, string refreshToken);
    Task<GameSession?> StartGameSessionAsync(string scenarioId, string accountId, string profileId, List<string> playerNames, string targetAgeGroup);
    Task<GameSession?> EndGameSessionAsync(string sessionId);
    Task<List<GameSession>?> GetSessionsByAccountAsync(string accountId);
    Task<Account?> GetAccountByEmailAsync(string email);
    
    // Character endpoints
    Task<Character?> GetCharacterAsync(string id);
    Task<List<Character>?> GetCharactersAsync();
    
    // Profile endpoints
    Task<UserProfile?> GetProfileAsync(string id);
    Task<UserProfile?> GetProfileByIdAsync(string id);
    Task<List<UserProfile>?> GetProfilesByAccountAsync(string accountId);
    Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request);
    Task<List<UserProfile>?> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request);
    Task<UserProfile?> UpdateProfileAsync(string id, UpdateUserProfileRequest request);
    Task<bool> DeleteProfileAsync(string id);
    
    // Game state endpoints
    Task<ScenarioGameStateResponse?> GetScenariosWithGameStateAsync(string accountId);
    Task<bool> CompleteScenarioForAccountAsync(string accountId, string scenarioId);
    
    string GetApiBaseAddress();
    
    string GetMediaResourceEndpointUrl(string mediaId);
}
