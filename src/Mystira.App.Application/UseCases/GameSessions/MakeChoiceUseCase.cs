using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for making a choice in a game session
/// </summary>
public class MakeChoiceUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MakeChoiceUseCase> _logger;

    public MakeChoiceUseCase(
        IGameSessionRepository repository,
        IScenarioRepository scenarioRepository,
        IUnitOfWork unitOfWork,
        ILogger<MakeChoiceUseCase> logger)
    {
        _repository = repository;
        _scenarioRepository = scenarioRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> ExecuteAsync(MakeChoiceRequest request)
    {
        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            return null;
        }

        if (session.Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot make choice in session with status {session.Status}");
        }

        var scenario = await _scenarioRepository.GetByIdAsync(session.ScenarioId);
        if (scenario == null)
        {
            throw new InvalidOperationException("Scenario not found for session");
        }

        var currentScene = scenario.Scenes.FirstOrDefault(s => s.Id == request.SceneId);
        if (currentScene == null)
        {
            throw new ArgumentException("Scene not found in scenario");
        }

        var branch = currentScene.Branches.FirstOrDefault(b => b.Choice == request.ChoiceText);
        if (branch == null)
        {
            throw new ArgumentException("Choice not found in scene");
        }

        var playerId = !string.IsNullOrWhiteSpace(request.PlayerId)
            ? request.PlayerId
            : session.ProfileId;

        var compassAxis = request.CompassAxis ?? branch.CompassChange?.Axis;
        var compassDelta = request.CompassDelta ?? branch.CompassChange?.Delta;
        var compassDirection = request.CompassDirection;

        var sessionChoice = new SessionChoice
        {
            SceneId = request.SceneId,
            SceneTitle = currentScene.Title,
            ChoiceText = request.ChoiceText,
            NextScene = request.NextSceneId,
            PlayerId = playerId ?? string.Empty,
            CompassAxis = compassAxis,
            CompassDirection = compassDirection,
            CompassDelta = compassDelta,
            ChosenAt = DateTime.UtcNow,
            EchoGenerated = branch.EchoLog,
            CompassChange = !string.IsNullOrWhiteSpace(compassAxis) && compassDelta.HasValue
                ? new CompassChange { Axis = compassAxis, Delta = compassDelta.Value }
                : null
        };

        session.ChoiceHistory.Add(sessionChoice);

        if (branch.EchoLog != null)
        {
            session.EchoHistory.Add(new EchoLog
            {
                EchoType = branch.EchoLog.EchoType,
                Description = branch.EchoLog.Description,
                Strength = branch.EchoLog.Strength,
                Timestamp = DateTime.UtcNow
            });
        }

        session.CompassValues ??= new Dictionary<string, CompassTracking>();
        foreach (var axis in scenario.CoreAxes)
        {
            if (!session.CompassValues.ContainsKey(axis.Value))
            {
                session.CompassValues[axis.Value] = new CompassTracking
                {
                    Axis = axis.Value,
                    CurrentValue = 0.0,
                    StartingValue = 0.0,
                    History = new List<CompassChange>(),
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        session.CurrentSceneId = request.NextSceneId;
        session.ElapsedTime = DateTime.UtcNow - session.StartTime;

        var nextScene = scenario.Scenes.FirstOrDefault(s => s.Id == request.NextSceneId);
        if (nextScene == null || (!nextScene.Branches.Any() && string.IsNullOrEmpty(nextScene.NextSceneId)))
        {
            session.Status = SessionStatus.Completed;
            session.EndTime = DateTime.UtcNow;
        }

        session.RecalculateCompassProgressFromHistory();

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Choice made in session {SessionId}: {ChoiceText} -> {NextScene} (PlayerId={PlayerId})",
            session.Id,
            request.ChoiceText,
            request.NextSceneId,
            playerId);

        return session;
    }
}
