using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

public interface IGameSessionApiService
{
    Task<GameSession> StartSessionAsync(StartGameSessionRequest request);
    Task<GameSession?> GetSessionAsync(string sessionId);
    Task<List<GameSessionResponse>> GetSessionsByAccountAsync(string accountId);
    Task<List<GameSessionResponse>> GetSessionsByProfileAsync(string profileId);
    Task<List<GameSessionResponse>> GetInProgressSessionsAsync(string accountId);
    Task<GameSession?> MakeChoiceAsync(MakeChoiceRequest request);
    Task<GameSession?> PauseSessionAsync(string sessionId);
    Task<GameSession?> ResumeSessionAsync(string sessionId);
    Task<GameSession?> EndSessionAsync(string sessionId);
    Task<GameSession?> ProgressToSceneAsync(ProgressSceneRequest request);
    Task<SessionStatsResponse?> GetSessionStatsAsync(string sessionId);
    Task<List<SessionAchievement>> CheckAchievementsAsync(string sessionId);
    Task<GameSession?> SelectCharacterAsync(string sessionId, string characterId);
    Task<bool> DeleteSessionAsync(string sessionId);
    Task<List<GameSession>> GetSessionsForProfileAsync(string profileId);
    Task<int> GetActiveSessionsCountAsync();
}