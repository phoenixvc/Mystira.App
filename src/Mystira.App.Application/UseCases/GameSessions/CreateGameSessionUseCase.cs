using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for creating a new game session
/// </summary>
public class CreateGameSessionUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateGameSessionUseCase> _logger;

    public CreateGameSessionUseCase(
        IGameSessionRepository repository,
        IScenarioRepository scenarioRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateGameSessionUseCase> logger)
    {
        _repository = repository;
        _scenarioRepository = scenarioRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession> ExecuteAsync(StartGameSessionRequest request)
    {
        // Validate scenario exists
        var scenario = await _scenarioRepository.GetByIdAsync(request.ScenarioId);
        if (scenario == null)
        {
            throw new ArgumentException($"Scenario not found: {request.ScenarioId}");
        }

        // Validate age appropriateness
        if (!IsAgeGroupCompatible(scenario.MinimumAge, request.TargetAgeGroup))
        {
            throw new ArgumentException($"Scenario minimum age ({scenario.MinimumAge}) exceeds target age group ({request.TargetAgeGroup})");
        }

        // Check for existing active sessions for this scenario and account
        var existingActiveSessions = (await _repository.GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId)).ToList();

        // Auto-complete any existing active sessions for this scenario
        if (existingActiveSessions.Any())
        {
            _logger.LogInformation("Found {Count} existing active session(s) for scenario {ScenarioId} and account {AccountId}. Completing them.",
                existingActiveSessions.Count, request.ScenarioId, request.AccountId);

            foreach (var existingSession in existingActiveSessions)
            {
                existingSession.Status = SessionStatus.Completed;
                existingSession.EndTime = DateTime.UtcNow;
                existingSession.ElapsedTime = existingSession.EndTime.Value - existingSession.StartTime;
                await _repository.UpdateAsync(existingSession);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // Check for existing InProgress session for this scenario and account
        var pausedSession = existingActiveSessions.FirstOrDefault(s => s.Status == SessionStatus.InProgress);

        if (pausedSession != null)
        {
            _logger.LogInformation("Found existing InProgress session {ExistingSessionId} for scenario {ScenarioId}, pausing it",
                pausedSession.Id, request.ScenarioId);

            pausedSession.Status = SessionStatus.Paused;
            pausedSession.IsPaused = true;
            pausedSession.PausedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(pausedSession);
        }

        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString(),
            ScenarioId = request.ScenarioId,
            AccountId = request.AccountId,
            ProfileId = request.ProfileId,
            PlayerNames = request.PlayerNames,
            Status = SessionStatus.InProgress,
            CurrentSceneId = scenario.Scenes.First().Id,
            StartTime = DateTime.UtcNow,
            TargetAgeGroupName = request.TargetAgeGroup,
            SceneCount = scenario.Scenes.Count
        };

        // Initialize compass tracking for scenario axes
        foreach (var axis in scenario.CoreAxes)
        {
            session.CompassValues[axis.Value] = new CompassTracking
            {
                Axis = axis.Value,
                CurrentValue = 0.0f,
                History = new List<CompassChange>(),
                LastUpdated = DateTime.UtcNow
            };
        }

        await _repository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Started new game session: {SessionId} for Account: {AccountId}, Profile: {ProfileId}",
            session.Id, session.AccountId, session.ProfileId);
        return session;
    }

    private static bool IsAgeGroupCompatible(int scenarioMinimumAge, string targetAgeGroup)
    {
        // Parse target age group to get its minimum age
        var targetAgeGroupObj = AgeGroup.Parse(targetAgeGroup);
        if (targetAgeGroupObj != null)
        {
            return scenarioMinimumAge <= targetAgeGroupObj.MinimumAge;
        }

        // Fallback: try to parse age range (e.g., "6-9" -> 6)
        if (targetAgeGroup.Contains('-'))
        {
            var parts = targetAgeGroup.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[0], out var minAge))
            {
                return scenarioMinimumAge <= minAge;
            }
        }

        // If we can't parse, assume compatible
        return true;
    }
}

