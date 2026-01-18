using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Shared.Extensions;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for checking and awarding session achievements
/// </summary>
public class CheckAchievementsUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IBadgeConfigurationRepository _badgeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckAchievementsUseCase> _logger;

    public CheckAchievementsUseCase(
        IGameSessionRepository repository,
        IBadgeConfigurationRepository badgeRepository,
        IUnitOfWork unitOfWork,
        ILogger<CheckAchievementsUseCase> logger)
    {
        _repository = repository;
        _badgeRepository = badgeRepository;
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

        // Fetch all badge configurations to check thresholds dynamically
        var badgeConfigs = await _badgeRepository.GetAllAsync();

        // Check compass threshold achievements
        foreach (var compassTracking in session.CompassValues.Values)
        {
            // Find badge configuration for this axis
            var axisBadge = badgeConfigs.FirstOrDefault(b => b.Axis.Equals(compassTracking.Axis, StringComparison.OrdinalIgnoreCase));

            // Use configured threshold or fallback to 3.0f if not found
            var threshold = axisBadge?.Threshold ?? 3.0f;

            if (Math.Abs(compassTracking.CurrentValue) >= threshold)
            {
                var achievementId = $"{session.Id}_{compassTracking.Axis}_threshold";
                if (!session.Achievements.Any(a => a.Id == achievementId))
                {
                    achievements.Add(new SessionAchievement
                    {
                        Id = achievementId,
                        Title = axisBadge?.Name ?? $"{compassTracking.Axis.Replace("_", " ").ToTitleCase()} Badge",
                        Description = axisBadge?.Message ?? $"Reached {compassTracking.Axis} threshold of {threshold}",
                        IconName = !string.IsNullOrEmpty(axisBadge?.ImageId) ? axisBadge.ImageId : $"badge_{compassTracking.Axis}",
                        Type = AchievementType.CompassThreshold,
                        CompassAxis = compassTracking.Axis,
                        ThresholdValue = threshold,
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

