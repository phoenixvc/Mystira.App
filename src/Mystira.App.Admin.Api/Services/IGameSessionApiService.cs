using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Domain.Models;
using ContractsGameSessionResponse = Mystira.App.Contracts.Responses.GameSessions.GameSessionResponse;
using ContractsMakeChoiceRequest = Mystira.App.Contracts.Requests.GameSessions.MakeChoiceRequest;
using ContractsSessionStatsResponse = Mystira.App.Contracts.Responses.GameSessions.SessionStatsResponse;
using ContractsStartGameSessionRequest = Mystira.App.Contracts.Requests.GameSessions.StartGameSessionRequest;

namespace Mystira.App.Admin.Api.Services;

public interface IGameSessionApiService
{
    Task<GameSession> StartSessionAsync(ContractsStartGameSessionRequest request);
    Task<GameSession?> GetSessionAsync(string sessionId);
    Task<List<ContractsGameSessionResponse>> GetSessionsByAccountAsync(string accountId);
    Task<List<ContractsGameSessionResponse>> GetSessionsByProfileAsync(string profileId);
    Task<GameSession?> MakeChoiceAsync(ContractsMakeChoiceRequest request);
    Task<GameSession?> PauseSessionAsync(string sessionId);
    Task<GameSession?> ResumeSessionAsync(string sessionId);
    Task<GameSession?> EndSessionAsync(string sessionId);
    Task<ContractsSessionStatsResponse?> GetSessionStatsAsync(string sessionId);
    Task<List<SessionAchievement>> CheckAchievementsAsync(string sessionId);
    Task<GameSession?> SelectCharacterAsync(string sessionId, string characterId);
    Task<bool> DeleteSessionAsync(string sessionId);
    Task<List<GameSession>> GetSessionsForProfileAsync(string profileId);
    Task<int> GetActiveSessionsCountAsync();
    Task<GameSession?> ProgressSessionSceneAsync(string sessionId, string newSceneId);
}
