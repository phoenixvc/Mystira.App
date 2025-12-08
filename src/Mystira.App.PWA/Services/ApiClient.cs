using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Main API client that composes domain-specific API clients
/// </summary>
public class ApiClient : IApiClient
{
    private readonly IScenarioApiClient _scenarioClient;
    private readonly IGameSessionApiClient _gameSessionClient;
    private readonly IUserProfileApiClient _userProfileClient;
    private readonly IAuthApiClient _authClient;
    private readonly IMediaApiClient _mediaClient;
    private readonly IAvatarApiClient _avatarClient;
    private readonly IContentBundleApiClient _contentBundleClient;
    private readonly ICharacterApiClient _characterClient;

    public ApiClient(
        IScenarioApiClient scenarioClient,
        IGameSessionApiClient gameSessionClient,
        IUserProfileApiClient userProfileClient,
        IAuthApiClient authClient,
        IMediaApiClient mediaClient,
        IAvatarApiClient avatarClient,
        IContentBundleApiClient contentBundleClient,
        ICharacterApiClient characterClient)
    {
        _scenarioClient = scenarioClient;
        _gameSessionClient = gameSessionClient;
        _userProfileClient = userProfileClient;
        _authClient = authClient;
        _mediaClient = mediaClient;
        _avatarClient = avatarClient;
        _contentBundleClient = contentBundleClient;
        _characterClient = characterClient;
    }

    // Scenario methods
    public Task<List<Scenario>> GetScenariosAsync() => _scenarioClient.GetScenariosAsync();
    public Task<Scenario?> GetScenarioAsync(string id) => _scenarioClient.GetScenarioAsync(id);
    public Task<Scene?> GetSceneAsync(string scenarioId, string sceneId) => _scenarioClient.GetSceneAsync(scenarioId, sceneId);
    public Task<ScenarioGameStateResponse?> GetScenariosWithGameStateAsync(string accountId) => _scenarioClient.GetScenariosWithGameStateAsync(accountId);
    public Task<bool> CompleteScenarioForAccountAsync(string accountId, string scenarioId) => _scenarioClient.CompleteScenarioForAccountAsync(accountId, scenarioId);

    // GameSession methods
    public Task<GameSession?> StartGameSessionAsync(string scenarioId, string accountId, string profileId, List<string> playerNames, string targetAgeGroup) =>
        _gameSessionClient.StartGameSessionAsync(scenarioId, accountId, profileId, playerNames, targetAgeGroup);

    public Task<GameSession?> StartGameSessionWithAssignmentsAsync(StartGameSessionRequest request)
    {
        return _gameSessionClient.StartGameSessionWithAssignmentsAsync(request);
    }

    public Task<GameSession?> EndGameSessionAsync(string sessionId) => _gameSessionClient.EndGameSessionAsync(sessionId);
    public Task<GameSession?> PauseGameSessionAsync(string sessionId) => _gameSessionClient.PauseGameSessionAsync(sessionId);
    public Task<GameSession?> ResumeGameSessionAsync(string sessionId) => _gameSessionClient.ResumeGameSessionAsync(sessionId);
    public Task<GameSession?> ProgressSessionSceneAsync(string sessionId, string sceneId) => _gameSessionClient.ProgressSessionSceneAsync(sessionId, sceneId);
    public Task<GameSession?> MakeChoiceAsync(string sessionId, string sceneId, string choiceText, string nextSceneId) => _gameSessionClient.MakeChoiceAsync(sessionId, sceneId, choiceText, nextSceneId);
    public Task<List<GameSession>?> GetSessionsByAccountAsync(string accountId) => _gameSessionClient.GetSessionsByAccountAsync(accountId);
    public Task<List<GameSession>?> GetInProgressSessionsAsync(string accountId) => _gameSessionClient.GetInProgressSessionsAsync(accountId);

    // UserProfile methods
    public Task<UserProfile?> GetProfileAsync(string id) => _userProfileClient.GetProfileAsync(id);
    public Task<UserProfile?> GetProfileByIdAsync(string id) => _userProfileClient.GetProfileByIdAsync(id);
    public Task<List<UserProfile>?> GetProfilesByAccountAsync(string accountId) => _userProfileClient.GetProfilesByAccountAsync(accountId);
    public Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request) => _userProfileClient.CreateProfileAsync(request);
    public Task<List<UserProfile>?> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request) => _userProfileClient.CreateMultipleProfilesAsync(request);
    public Task<UserProfile?> UpdateProfileAsync(string id, UpdateUserProfileRequest request) => _userProfileClient.UpdateProfileAsync(id, request);
    public Task<bool> DeleteProfileAsync(string id) => _userProfileClient.DeleteProfileAsync(id);

    // Auth methods
    public Task<PasswordlessSignupResponse?> RequestPasswordlessSignupAsync(string email, string displayName) =>
        _authClient.RequestPasswordlessSignupAsync(email, displayName);
    public Task<PasswordlessVerifyResponse?> VerifyPasswordlessSignupAsync(string email, string code) =>
        _authClient.VerifyPasswordlessSignupAsync(email, code);
    public Task<PasswordlessSigninResponse?> RequestPasswordlessSigninAsync(string email) =>
        _authClient.RequestPasswordlessSigninAsync(email);
    public Task<PasswordlessVerifyResponse?> VerifyPasswordlessSigninAsync(string email, string code) =>
        _authClient.VerifyPasswordlessSigninAsync(email, code);
    public Task<RefreshTokenResponse?> RefreshTokenAsync(string token, string refreshToken) =>
        _authClient.RefreshTokenAsync(token, refreshToken);
    public Task<Account?> GetAccountByEmailAsync(string email) => _authClient.GetAccountByEmailAsync(email);

    // Media methods
    public Task<string?> GetMediaUrlFromId(string mediaId) => _mediaClient.GetMediaUrlFromId(mediaId);
    public string GetMediaResourceEndpointUrl(string mediaId) => _mediaClient.GetMediaResourceEndpointUrl(mediaId);

    // Avatar methods
    public Task<Dictionary<string, List<string>>?> GetAvatarsAsync() => _avatarClient.GetAvatarsAsync();
    public Task<List<string>?> GetAvatarsByAgeGroupAsync(string ageGroup) => _avatarClient.GetAvatarsByAgeGroupAsync(ageGroup);

    // ContentBundle methods
    public Task<List<ContentBundle>> GetBundlesAsync() => _contentBundleClient.GetBundlesAsync();
    public Task<List<ContentBundle>> GetBundlesByAgeGroupAsync(string ageGroup) => _contentBundleClient.GetBundlesByAgeGroupAsync(ageGroup);

    // Character methods
    public Task<Character?> GetCharacterAsync(string id) => _characterClient.GetCharacterAsync(id);
    public Task<List<Character>?> GetCharactersAsync() => _characterClient.GetCharactersAsync();

    // Utility methods
    public string GetApiBaseAddress()
    {
        // Get base address from media client (all clients share the same HttpClient base address)
        var mediaUrl = _mediaClient.GetMediaResourceEndpointUrl("test");
        return mediaUrl.Replace("api/media/test", "");
    }
}
