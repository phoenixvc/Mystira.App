using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Services;

/// <summary>
/// Scores completed game sessions by aggregating per-choice axis deltas
/// </summary>
public class AxisScoringService : IAxisScoringService
{
    private readonly IPlayerScenarioScoreRepository _scoreRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AxisScoringService> _logger;

    public AxisScoringService(
        IPlayerScenarioScoreRepository scoreRepository,
        IUnitOfWork unitOfWork,
        ILogger<AxisScoringService> logger)
    {
        _scoreRepository = scoreRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PlayerScenarioScore?> ScoreSessionAsync(GameSession session, UserProfile profile)
    {
        // Check if this profile/scenario pair has already been scored
        var existingScore = await _scoreRepository.GetByProfileAndScenarioAsync(profile.Id, session.ScenarioId);
        if (existingScore != null)
        {
            _logger.LogInformation(
                "Scenario {ScenarioId} already scored for profile {ProfileId}. Skipping replay scoring.",
                session.ScenarioId, profile.Id);
            return null;
        }

        // Aggregate axis scores from the session's choice history
        var axisScores = AggregateAxisScores(session, profile.Id);

        // Create the score record
        var playerScore = new PlayerScenarioScore
        {
            ProfileId = profile.Id,
            ScenarioId = session.ScenarioId,
            GameSessionId = session.Id,
            AxisScores = axisScores,
            CreatedAt = DateTime.UtcNow
        };

        // Persist the score
        await _scoreRepository.AddAsync(playerScore);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation(
            "Scored session {SessionId} for profile {ProfileId} on scenario {ScenarioId}. Axes: {@AxisScores}",
            session.Id, profile.Id, session.ScenarioId, axisScores);

        return playerScore;
    }

    private Dictionary<string, float> AggregateAxisScores(GameSession session, string profileId)
    {
        var axisScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        foreach (var choice in session.ChoiceHistory)
        {
            if (choice.PlayerId != profileId) continue;

            if (!TryGetCompassDelta(choice, out var axis, out var delta)) continue;

            if (!axisScores.ContainsKey(axis)) axisScores[axis] = 0f;

            axisScores[axis] += (float)delta;
        }

        return axisScores;
    }

    private static bool TryGetCompassDelta(SessionChoice choice, out string axis, out double delta)
    {
        axis = string.Empty;
        delta = 0.0;

        if (!string.IsNullOrWhiteSpace(choice.CompassAxis) && choice.CompassDelta.HasValue)
        {
            axis = choice.CompassAxis;
            delta = choice.CompassDelta.Value;
            return true;
        }

        if (choice.CompassChange != null && !string.IsNullOrWhiteSpace(choice.CompassChange.Axis))
        {
            axis = choice.CompassChange.Axis;
            delta = choice.CompassChange.Delta;
            return true;
        }

        return false;
    }
}
