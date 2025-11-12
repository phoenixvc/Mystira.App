using Mystira.App.Domain.Models;
using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IGameSessionApiService
{
    Task<GameSession> StartSessionAsync(StartGameSessionRequest request);
    Task<GameSession?> GetSessionAsync(string sessionId);
    Task<List<GameSessionResponse>> GetSessionsByDmAsync(string dmName);
    Task<GameSession?> MakeChoiceAsync(MakeChoiceRequest request);
    Task<GameSession?> PauseSessionAsync(string sessionId);
    Task<GameSession?> ResumeSessionAsync(string sessionId);
    Task<GameSession?> EndSessionAsync(string sessionId);
    Task<SessionStatsResponse?> GetSessionStatsAsync(string sessionId);
    Task<List<SessionAchievement>> CheckAchievementsAsync(string sessionId);
    Task<GameSession?> SelectCharacterAsync(string sessionId, string characterId);
    Task<bool> DeleteSessionAsync(string sessionId);
    Task<List<GameSession>> GetSessionsForProfileAsync(string profileId);
    Task<int> GetActiveSessionsCountAsync();
}