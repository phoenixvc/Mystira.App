using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for checking and awarding session achievements
/// </summary>
public class CheckAchievementsUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckAchievementsUseCase> _logger;

    public CheckAchievementsUseCase(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CheckAchievementsUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<SessionAchievement>> ExecuteAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Game session not found: {SessionId}", sessionId);
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

        // Add new achievements to session and save
        if (achievements.Any())
        {
            foreach (var achievement in achievements)
            {
                session.Achievements.Add(achievement);
            }

            await _repository.UpdateAsync(session);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Awarded {Count} achievements to session {SessionId}", achievements.Count, sessionId);
        }

        return achievements;
    }
}

// Extension method for title case conversion
internal static class StringExtensions
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

