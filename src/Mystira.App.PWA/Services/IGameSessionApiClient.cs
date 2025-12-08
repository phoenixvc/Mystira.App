using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IGameSessionApiClient
{
    Task<GameSession?> StartGameSessionAsync(string scenarioId, string accountId, string profileId, List<string> playerNames, string targetAgeGroup);
    Task<GameSession?> EndGameSessionAsync(string sessionId);
    Task<GameSession?> PauseGameSessionAsync(string sessionId);
    Task<GameSession?> ResumeGameSessionAsync(string sessionId);
    Task<GameSession?> ProgressSessionSceneAsync(string sessionId, string sceneId);
    Task<List<GameSession>?> GetSessionsByAccountAsync(string accountId);
    Task<List<GameSession>?> GetInProgressSessionsAsync(string accountId);
    Task<GameSession?> MakeChoiceAsync(string sessionId, string sceneId, string choiceText, string nextSceneId);
}

