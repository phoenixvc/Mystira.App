using Mystira.App.Application.UseCases.GameSessions;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

public class GameSessionApiService : IGameSessionApiService
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScenarioApiService _scenarioService;
    private readonly ILogger<GameSessionApiService> _logger;
    private readonly CreateGameSessionUseCase _createGameSessionUseCase;
    private readonly MakeChoiceUseCase _makeChoiceUseCase;
    private readonly ProgressSceneUseCase _progressSceneUseCase;

    public GameSessionApiService(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        IScenarioApiService scenarioService,
        ILogger<GameSessionApiService> logger,
        CreateGameSessionUseCase createGameSessionUseCase,
        MakeChoiceUseCase makeChoiceUseCase,
        ProgressSceneUseCase progressSceneUseCase)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scenarioService = scenarioService;
        _logger = logger;
        _createGameSessionUseCase = createGameSessionUseCase;
        _makeChoiceUseCase = makeChoiceUseCase;
        _progressSceneUseCase = progressSceneUseCase;
    }

    public async Task<GameSession> StartSessionAsync(StartGameSessionRequest request)
    {
        return await _createGameSessionUseCase.ExecuteAsync(request);
    }

    public async Task<GameSession?> GetSessionAsync(string sessionId)
    {
        return await _repository.GetByIdAsync(sessionId);
    }

    public async Task<List<GameSessionResponse>> GetSessionsByAccountAsync(string accountId)
    {
        var sessions = await _repository.GetByAccountIdAsync(accountId);
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
        var sessions = await _repository.GetByProfileIdAsync(profileId);
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
        var sessions = await _repository.GetInProgressSessionsAsync(accountId);
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

        // Service-specific logic: Check for achievements
        var newAchievements = await CheckAchievementsAsync(session.Id);
        if (newAchievements.Any())
        {
            // Reload session to get latest state after use case execution
            session = await _repository.GetByIdAsync(request.SessionId);
            if (session != null)
            {
                foreach (var achievement in newAchievements.Where(a => !session.Achievements.Any(sa => sa.Id == a.Id)))
                {
                    session.Achievements.Add(achievement);
                }

                await _repository.UpdateAsync(session);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return session;
    }

    public async Task<GameSession?> PauseSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        if (session.Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException("Can only pause sessions in progress");
        }

        session.Status = SessionStatus.Paused;
        session.IsPaused = true;
        session.PausedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Paused session: {SessionId}", sessionId);
        return session;
    }

    public async Task<GameSession?> ResumeSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        if (session.Status != SessionStatus.Paused)
        {
            throw new InvalidOperationException("Can only resume paused sessions");
        }

        session.Status = SessionStatus.InProgress;
        session.IsPaused = false;
        session.PausedAt = null;

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Resumed session: {SessionId}", sessionId);
        return session;
    }

    public async Task<GameSession?> EndSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        session.Status = SessionStatus.Completed;
        session.EndTime = DateTime.UtcNow;
        session.ElapsedTime = session.EndTime.Value - session.StartTime;
        session.IsPaused = false;

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Ended session: {SessionId}", sessionId);
        return session;
    }

    public async Task<GameSession?> ProgressToSceneAsync(ProgressSceneRequest request)
    {
        return await _progressSceneUseCase.ExecuteAsync(request);
    }

    public async Task<SessionStatsResponse?> GetSessionStatsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        var compassValues = session.CompassValues.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CurrentValue
        );

        var recentEchoes = session.EchoHistory
            .OrderByDescending(e => e.Timestamp)
            .Take(5)
            .ToList();

        return new SessionStatsResponse
        {
            CompassValues = compassValues,
            RecentEchoes = recentEchoes,
            Achievements = session.Achievements,
            TotalChoices = session.ChoiceHistory.Count,
            SessionDuration = session.EndTime?.Subtract(session.StartTime) ?? DateTime.UtcNow.Subtract(session.StartTime)
        };
    }

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
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return false;
        }

        await _repository.DeleteAsync(sessionId);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted session: {SessionId}", sessionId);
        return true;
    }

    public async Task<GameSession?> SelectCharacterAsync(string sessionId, string characterId)
    {
        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        session.SelectedCharacterId = characterId;
        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Selected character {CharacterId} for session {SessionId}", characterId, sessionId);
        return session;
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

            var sessions = await _repository.GetByProfileIdAsync(profileId);

            return sessions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for profile {ProfileId}", profileId);
            return new List<GameSession>();
        }
    }


    public async Task<int> GetActiveSessionsCountAsync()
    {
        try
        {
            return await _repository.GetActiveSessionsCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions count");
            return 0;
        }
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

            // Update the current scene
            session.CurrentSceneId = newSceneId;
            session.ElapsedTime = DateTime.UtcNow - session.StartTime;

            await _repository.UpdateAsync(session);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Progressed session {SessionId} to scene {SceneId}", sessionId, newSceneId);
            return session;
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
