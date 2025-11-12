using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IApiClient
{
    Task<List<Scenario>> GetScenariosAsync();
    Task<Scenario?> GetScenarioAsync(string id);
    Task<Scene?> GetSceneAsync(string scenarioId, string sceneId);
    Task<string?> GetMediaUrlFromId(string mediaAudio);
    Task<PasswordlessSignupResponse?> RequestPasswordlessSignupAsync(string email, string displayName);
    Task<PasswordlessVerifyResponse?> VerifyPasswordlessSignupAsync(string email, string code);
    Task<PasswordlessSigninResponse?> RequestPasswordlessSigninAsync(string email);
    Task<PasswordlessVerifyResponse?> VerifyPasswordlessSigninAsync(string email, string code);
    Task<GameSession?> StartGameSessionAsync(string scenarioId, string accountId, string profileId, List<string> playerNames, string targetAgeGroup);
    Task<Account?> GetAccountByEmailAsync(string email);
}
