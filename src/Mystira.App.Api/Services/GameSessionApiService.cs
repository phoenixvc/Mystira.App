using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Data;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

public class GameSessionApiService : IGameSessionApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly IScenarioApiService _scenarioService;
    private readonly ILogger<GameSessionApiService> _logger;

    public GameSessionApiService(
        MystiraAppDbContext context,
        IScenarioApiService scenarioService,
        ILogger<GameSessionApiService> logger)
    {
        _context = context;
        _scenarioService = scenarioService;
        _logger = logger;
    }

    public async Task<GameSession> StartSessionAsync(StartGameSessionRequest request)
    {
        // Validate scenario exists
        var scenario = await _scenarioService.GetScenarioByIdAsync(request.ScenarioId);
        if (scenario == null)
            throw new ArgumentException($"Scenario not found: {request.ScenarioId}");

        // Validate age appropriateness
        if (!IsAgeGroupCompatible(scenario.MinimumAge, request.TargetAgeGroup))
            throw new ArgumentException($"Scenario minimum age ({scenario.MinimumAge}) exceeds target age group ({request.TargetAgeGroup})");

        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString(),
            ScenarioId = request.ScenarioId,
            DmName = request.DmName,
            PlayerNames = request.PlayerNames,
            Status = SessionStatus.InProgress,
            CurrentSceneId = scenario.Scenes.First().Id,
            StartTime = DateTime.UtcNow,
            TargetAgeGroupName = request.TargetAgeGroup,
            SceneCount = scenario.Scenes.Count
        };

        // Initialize compass tracking for scenario axes
        foreach (var axis in scenario.CompassAxes)
        {
            session.CompassValues[axis] = new CompassTracking
            {
                Axis = axis,
                CurrentValue = 0.0f,
                History = new List<CompassChange>(),
                LastUpdated = DateTime.UtcNow
            };
        }

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Started new game session: {SessionId} for DM: {DmName}", session.Id, session.DmName);
        return session;
    }

    public async Task<GameSession?> GetSessionAsync(string sessionId)
    {
        return await _context.GameSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<List<GameSessionResponse>> GetSessionsByDmAsync(string dmName)
    {
        return await _context.GameSessions
            .Where(s => s.DmName == dmName)
            .OrderByDescending(s => s.StartTime)
            .Select(s => new GameSessionResponse
            {
                Id = s.Id,
                ScenarioId = s.ScenarioId,
                DmName = s.DmName,
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
            })
            .ToListAsync();
    }

    public async Task<GameSession?> MakeChoiceAsync(MakeChoiceRequest request)
    {
        var session = await GetSessionAsync(request.SessionId);
        if (session == null)
            return null;

        if (session.Status != SessionStatus.InProgress)
            throw new InvalidOperationException("Cannot make choice in non-active session");

        var scenario = await _scenarioService.GetScenarioByIdAsync(session.ScenarioId);
        if (scenario == null)
            throw new InvalidOperationException("Scenario not found for session");

        var currentScene = scenario.Scenes.FirstOrDefault(s => s.Id == request.SceneId);
        if (currentScene == null)
            throw new ArgumentException("Scene not found in scenario");

        var branch = currentScene.Branches.FirstOrDefault(b => b.Choice == request.ChoiceText);
        if (branch == null)
            throw new ArgumentException("Choice not found in scene");

        // Record the choice
        var sessionChoice = new SessionChoice
        {
            SceneId = request.SceneId,
            SceneTitle = currentScene.Title,
            ChoiceText = request.ChoiceText,
            NextSceneId = request.NextSceneId,
            ChosenAt = DateTime.UtcNow,
            EchoGenerated = branch.EchoLog,
            CompassChange = branch.CompassChange
        };

        session.ChoiceHistory.Add(sessionChoice);

        // Process echo log if present
        if (branch.EchoLog != null)
        {
            var echo = new EchoLog
            {
                EchoType = branch.EchoLog.EchoType,
                Description = branch.EchoLog.Description,
                Strength = branch.EchoLog.Strength,
                Timestamp = DateTime.UtcNow
            };
            session.EchoHistory.Add(echo);
        }

        // Process compass change if present
        if (branch.CompassChange != null && session.CompassValues.ContainsKey(branch.CompassChange.Axis))
        {
            var tracking = session.CompassValues[branch.CompassChange.Axis];
            tracking.CurrentValue += branch.CompassChange.Delta;
            tracking.CurrentValue = Math.Max(-2.0f, Math.Min(2.0f, tracking.CurrentValue)); // Clamp to [-2, 2]
            
            var compassChange = new CompassChange
            {
                Axis = branch.CompassChange.Axis,
                Delta = branch.CompassChange.Delta
            };
            tracking.History.Add(compassChange);
            tracking.LastUpdated = DateTime.UtcNow;
        }

        // Update session state
        session.CurrentSceneId = request.NextSceneId;
        session.ElapsedTime = DateTime.UtcNow - session.StartTime;

        // Check for achievements
        var newAchievements = await CheckAchievementsAsync(session.Id);
        foreach (var achievement in newAchievements.Where(a => !session.Achievements.Any(sa => sa.Id == a.Id)))
        {
            session.Achievements.Add(achievement);
        }

        // Check if session is complete (reached end scene or no more branches)
        var nextScene = scenario.Scenes.FirstOrDefault(s => s.Id == request.NextSceneId);
        if (nextScene == null || !nextScene.Branches.Any())
        {
            session.Status = SessionStatus.Completed;
            session.EndTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Choice made in session {SessionId}: {ChoiceText} -> {NextSceneId}", 
            session.Id, request.ChoiceText, request.NextSceneId);

        return session;
    }

    public async Task<GameSession?> PauseSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
            return null;

        if (session.Status != SessionStatus.InProgress)
            throw new InvalidOperationException("Can only pause sessions in progress");

        session.Status = SessionStatus.Paused;
        session.IsPaused = true;
        session.PausedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Paused session: {SessionId}", sessionId);
        return session;
    }

    public async Task<GameSession?> ResumeSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
            return null;

        if (session.Status != SessionStatus.Paused)
            throw new InvalidOperationException("Can only resume paused sessions");

        session.Status = SessionStatus.InProgress;
        session.IsPaused = false;
        session.PausedAt = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Resumed session: {SessionId}", sessionId);
        return session;
    }

    public async Task<GameSession?> EndSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
            return null;

        session.Status = SessionStatus.Completed;
        session.EndTime = DateTime.UtcNow;
        session.ElapsedTime = session.EndTime.Value - session.StartTime;
        session.IsPaused = false;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ended session: {SessionId}", sessionId);
        return session;
    }

    public async Task<SessionStatsResponse?> GetSessionStatsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
            return null;

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
            return new List<SessionAchievement>();

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
            return false;

        _context.GameSessions.Remove(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted session: {SessionId}", sessionId);
        return true;
    }

    public async Task<GameSession?> SelectCharacterAsync(string sessionId, string characterId)
    {
        var session = await _context.GameSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
            return null;

        session.SelectedCharacterId = characterId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Selected character {CharacterId} for session {SessionId}", characterId, sessionId);
        return session;
    }

    public async Task<List<GameSession>> GetSessionsForProfileAsync(string profileId)
    {
        try
        {
            // Game sessions can be linked to profiles in multiple ways:
            // 1. By DM name (if the profile owner is the DM)
            // 2. By player names (if the profile is a player)
            // 3. By a direct profile relationship (if we had such a field)
            
            // For now, we'll search by matching the profile name with DM name or player names
            // This is a simplification - in practice, you might want to add a more direct relationship
            
            var sessions = await _context.GameSessions
                .Where(s => s.DmName == profileId || s.PlayerNames.Contains(profileId))
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for profile {ProfileId}", profileId);
            return new List<GameSession>();
        }
    }

    private bool IsAgeGroupCompatible(string minimumAge, string targetAge)
    {
        // Define age group hierarchy (from youngest to oldest)
        var ageOrder = new List<string> 
        { 
            AgeGroup.Toddlers.Name, 
            AgeGroup.Preschoolers.Name, 
            AgeGroup.School.Name, 
            AgeGroup.Preteens.Name, 
            AgeGroup.Teens.Name 
        };

        var minIndex = ageOrder.IndexOf(minimumAge);
        var targetIndex = ageOrder.IndexOf(targetAge);

        // If either age group is not found, assume compatible for backward compatibility
        if (minIndex == -1 || targetIndex == -1)
            return true;

        // Target age group must be at or above the minimum age group
        return targetIndex >= minIndex;
    }

    public async Task<int> GetActiveSessionsCountAsync()
    {
        try
        {
            return await _context.GameSessions
                .CountAsync(s => s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions count");
            return 0;
        }
    }
}

// Extension method for title case conversion
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

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