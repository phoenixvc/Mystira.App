using Mystira.App.Application.UseCases.GameSessions;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

public class GameSessionApiService : IGameSessionApiService
{
    private readonly IScenarioApiService _scenarioService;
    private readonly ILogger<GameSessionApiService> _logger;
    private readonly CreateGameSessionUseCase _createGameSessionUseCase;
    private readonly GetGameSessionUseCase _getGameSessionUseCase;
    private readonly GetGameSessionsByAccountUseCase _getGameSessionsByAccountUseCase;
    private readonly GetGameSessionsByProfileUseCase _getGameSessionsByProfileUseCase;
    private readonly GetInProgressSessionsUseCase _getInProgressSessionsUseCase;
    private readonly MakeChoiceUseCase _makeChoiceUseCase;
    private readonly ProgressSceneUseCase _progressSceneUseCase;
    private readonly PauseGameSessionUseCase _pauseGameSessionUseCase;
    private readonly ResumeGameSessionUseCase _resumeGameSessionUseCase;
    private readonly EndGameSessionUseCase _endGameSessionUseCase;
    private readonly SelectCharacterUseCase _selectCharacterUseCase;
    private readonly GetSessionStatsUseCase _getSessionStatsUseCase;
    private readonly CheckAchievementsUseCase _checkAchievementsUseCase;
    private readonly DeleteGameSessionUseCase _deleteGameSessionUseCase;

    public GameSessionApiService(
        IScenarioApiService scenarioService,
        ILogger<GameSessionApiService> logger,
        CreateGameSessionUseCase createGameSessionUseCase,
        GetGameSessionUseCase getGameSessionUseCase,
        GetGameSessionsByAccountUseCase getGameSessionsByAccountUseCase,
        GetGameSessionsByProfileUseCase getGameSessionsByProfileUseCase,
        GetInProgressSessionsUseCase getInProgressSessionsUseCase,
        MakeChoiceUseCase makeChoiceUseCase,
        ProgressSceneUseCase progressSceneUseCase,
        PauseGameSessionUseCase pauseGameSessionUseCase,
        ResumeGameSessionUseCase resumeGameSessionUseCase,
        EndGameSessionUseCase endGameSessionUseCase,
        SelectCharacterUseCase selectCharacterUseCase,
        GetSessionStatsUseCase getSessionStatsUseCase,
        CheckAchievementsUseCase checkAchievementsUseCase,
        DeleteGameSessionUseCase deleteGameSessionUseCase)
    {
        _scenarioService = scenarioService;
        _logger = logger;
        _createGameSessionUseCase = createGameSessionUseCase;
        _getGameSessionUseCase = getGameSessionUseCase;
        _getGameSessionsByAccountUseCase = getGameSessionsByAccountUseCase;
        _getGameSessionsByProfileUseCase = getGameSessionsByProfileUseCase;
        _getInProgressSessionsUseCase = getInProgressSessionsUseCase;
        _makeChoiceUseCase = makeChoiceUseCase;
        _progressSceneUseCase = progressSceneUseCase;
        _pauseGameSessionUseCase = pauseGameSessionUseCase;
        _resumeGameSessionUseCase = resumeGameSessionUseCase;
        _endGameSessionUseCase = endGameSessionUseCase;
        _selectCharacterUseCase = selectCharacterUseCase;
        _getSessionStatsUseCase = getSessionStatsUseCase;
        _checkAchievementsUseCase = checkAchievementsUseCase;
        _deleteGameSessionUseCase = deleteGameSessionUseCase;
    }

    public async Task<GameSession> StartSessionAsync(StartGameSessionRequest request)
    {
        return await _createGameSessionUseCase.ExecuteAsync(request);
    }

    public async Task<GameSession?> GetSessionAsync(string sessionId)
    {
        return await _getGameSessionUseCase.ExecuteAsync(sessionId);
    }

    public async Task<List<GameSessionResponse>> GetSessionsByAccountAsync(string accountId)
    {
        var sessions = await _getGameSessionsByAccountUseCase.ExecuteAsync(accountId);
        return sessions.Select(s => new GameSessionResponse
        {
            Id = s.Id,
            ScenarioId = s.ScenarioId,
            AccountId = s.AccountId,
            ProfileId = s.ProfileId,
            PlayerNames = s.PlayerNames,
            Status = s.Status,
            CurrentSceneId = s.CurrentSceneId,
            ChoiceCount = s.ChoiceHistory.Count,
            EchoCount = s.EchoHistory.Count,
            AchievementCount = s.Achievements.Count,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            ElapsedTime = s.ElapsedTime,
            IsPaused = s.IsPaused,
            SceneCount = s.SceneCount,
            TargetAgeGroup = s.TargetAgeGroupName
        }).ToList();
    }

    public async Task<List<GameSessionResponse>> GetSessionsByProfileAsync(string profileId)
    {
        var sessions = await _getGameSessionsByProfileUseCase.ExecuteAsync(profileId);
        return sessions.Select(s => new GameSessionResponse
        {
            Id = s.Id,
            ScenarioId = s.ScenarioId,
            AccountId = s.AccountId,
            ProfileId = s.ProfileId,
            PlayerNames = s.PlayerNames,
            Status = s.Status,
            CurrentSceneId = s.CurrentSceneId,
            ChoiceCount = s.ChoiceHistory.Count,
            EchoCount = s.EchoHistory.Count,
            AchievementCount = s.Achievements.Count,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            ElapsedTime = s.ElapsedTime,
            IsPaused = s.IsPaused,
            SceneCount = s.SceneCount,
            TargetAgeGroup = s.TargetAgeGroupName
        }).ToList();
    }

    public async Task<List<GameSessionResponse>> GetInProgressSessionsAsync(string accountId)
    {
        var sessions = await _getInProgressSessionsUseCase.ExecuteAsync(accountId);
        return sessions.Select(s => new GameSessionResponse
        {
            Id = s.Id,
            ScenarioId = s.ScenarioId,
            AccountId = s.AccountId,
            ProfileId = s.ProfileId,
            PlayerNames = s.PlayerNames,
            Status = s.Status,
            CurrentSceneId = s.CurrentSceneId,
            ChoiceCount = s.ChoiceHistory.Count,
            EchoCount = s.EchoHistory.Count,
            AchievementCount = s.Achievements.Count,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            ElapsedTime = s.ElapsedTime,
            IsPaused = s.IsPaused,
            SceneCount = s.SceneCount,
            TargetAgeGroup = s.TargetAgeGroupName
        }).ToList();
    }

    public async Task<GameSession?> MakeChoiceAsync(MakeChoiceRequest request)
    {
        // Delegate core business logic to use case
        var session = await _makeChoiceUseCase.ExecuteAsync(request);
        if (session == null)
        {
            return null;
        }

        // Check for achievements after making choice
        var newAchievements = await _checkAchievementsUseCase.ExecuteAsync(session.Id);
        if (newAchievements.Any())
        {
            // Achievements are already added to session by CheckAchievementsUseCase
            // Just merge them into the returned session
            foreach (var achievement in newAchievements.Where(a => !session.Achievements.Any(sa => sa.Id == a.Id)))
            {
                session.Achievements.Add(achievement);
            }
        }

        return session;
    }

    public async Task<GameSession?> PauseSessionAsync(string sessionId)
    {
        try
        {
            return await _pauseGameSessionUseCase.ExecuteAsync(sessionId);
        }
        catch (ArgumentException)
        {
            return null; // Session not found
        }
    }

    public async Task<GameSession?> ResumeSessionAsync(string sessionId)
    {
        try
        {
            return await _resumeGameSessionUseCase.ExecuteAsync(sessionId);
        }
        catch (ArgumentException)
        {
            return null; // Session not found
        }
    }

    public async Task<GameSession?> EndSessionAsync(string sessionId)
    {
        try
        {
            return await _endGameSessionUseCase.ExecuteAsync(sessionId);
        }
        catch (ArgumentException)
        {
            return null; // Session not found
        }
    }

    public async Task<GameSession?> ProgressToSceneAsync(ProgressSceneRequest request)
    {
        return await _progressSceneUseCase.ExecuteAsync(request);
    }

    public Task<SessionStatsResponse?> GetSessionStatsAsync(string sessionId) =>
        _getSessionStatsUseCase.ExecuteAsync(sessionId);

    public async Task<List<SessionAchievement>> CheckAchievementsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return new List<SessionAchievement>();
        }

        var achievements = new List<SessionAchievement>();
        // TODO: Replace with BadgeConfigurationApiService to get dynamic badge thresholds
        // For now, using simple threshold logic as placeholder
        var defaultThreshold = 3.0f;

        // Check compass threshold achievements
        foreach (var compassTracking in session.CompassValues.Values)
        {
            if (Math.Abs(compassTracking.CurrentValue) >= defaultThreshold)
            {
                var achievementId = $"{session.Id}_{compassTracking.Axis}_threshold";
                if (!session.Achievements.Any(a => a.Id == achievementId))
                {
                    achievements.Add(new SessionAchievement
                    {
                        Id = achievementId,
                        Title = $"{compassTracking.Axis.Replace("_", " ").ToTitleCase()} Badge",
                        Description = $"Reached {compassTracking.Axis} threshold of {defaultThreshold}",
                        IconName = $"badge_{compassTracking.Axis}",
                        Type = AchievementType.CompassThreshold,
                        CompassAxis = compassTracking.Axis,
                        ThresholdValue = defaultThreshold,
                        EarnedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // Check first choice achievement
        if (session.ChoiceHistory.Count == 1)
        {
            var firstChoiceId = $"{session.Id}_first_choice";
            if (!session.Achievements.Any(a => a.Id == firstChoiceId))
            {
                achievements.Add(new SessionAchievement
                {
                    Id = firstChoiceId,
                    Title = "First Steps",
                    Description = "Made your first choice in the adventure",
                    IconName = "badge_first_choice",
                    Type = AchievementType.FirstChoice,
                    EarnedAt = DateTime.UtcNow
                });
            }
        }

        // Check session completion achievement
        if (session.Status == SessionStatus.Completed)
        {
            var completionId = $"{session.Id}_completion";
            if (!session.Achievements.Any(a => a.Id == completionId))
            {
                achievements.Add(new SessionAchievement
                {
                    Id = completionId,
                    Title = "Adventure Complete",
                    Description = "Successfully completed the adventure",
                    IconName = "badge_completion",
                    Type = AchievementType.SessionComplete,
                    EarnedAt = DateTime.UtcNow
                });
            }
        }

        return achievements;
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        try
        {
            return await _deleteGameSessionUseCase.ExecuteAsync(sessionId);
        }
        catch (ArgumentException)
        {
            return false; // Session not found
        }
    }

    public async Task<GameSession?> SelectCharacterAsync(string sessionId, string characterId)
    {
        try
        {
            return await _selectCharacterUseCase.ExecuteAsync(sessionId, characterId);
        }
        catch (ArgumentException)
        {
            return null; // Session not found
        }
    }

    public async Task<List<GameSession>> GetSessionsForProfileAsync(string profileId)
    {
        try
        {
            // Game sessions can be linked to profiles in multiple ways:
            // 1. By account ID (if the profile owner is the account holder)
            // 2. By player names (if the profile is a player)
            // 3. By a direct profile relationship (if we had such a field)

            // For now, we'll search by matching the profile name with player names
            // This is a simplification - in practice, you might want to add a more direct relationship

            var sessions = await _getGameSessionsByProfileUseCase.ExecuteAsync(profileId);
            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for profile {ProfileId}", profileId);
            return new List<GameSession>();
        }
    }


    public Task<int> GetActiveSessionsCountAsync()
    {
        // Note: GetActiveSessionsCountAsync needs a dedicated use case
        // For now, this method is not implemented as it requires repository access
        // TODO: Create GetActiveSessionsCountUseCase when this functionality is needed
        _logger.LogWarning("GetActiveSessionsCountAsync is not implemented. Use GetInProgressSessionsUseCase for account-specific queries.");
        return Task.FromException<int>(new NotImplementedException("GetActiveSessionsCountAsync requires a dedicated use case. Use GetInProgressSessionsUseCase for account-specific queries."));
    }

    public async Task<GameSession?> ProgressSessionSceneAsync(string sessionId, string newSceneId)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                return null;
            }

            if (session.Status != SessionStatus.InProgress)
            {
                _logger.LogWarning("Cannot progress scene for session {SessionId} with status {Status}",
                    sessionId, session.Status);
                return null;
            }

            // Use ProgressSceneUseCase instead of direct repository access
            var progressRequest = new ProgressSceneRequest
            {
                SessionId = sessionId,
                SceneId = newSceneId
            };
            var updatedSession = await _progressSceneUseCase.ExecuteAsync(progressRequest);
            _logger.LogInformation("Progressed session {SessionId} to scene {SceneId}", sessionId, newSceneId);
            return updatedSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error progressing session {SessionId} to scene {SceneId}", sessionId, newSceneId);
            return null;
        }
    }
}

// Extension method for title case conversion
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var words = input.Split(' ', '_', '-');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }
}
