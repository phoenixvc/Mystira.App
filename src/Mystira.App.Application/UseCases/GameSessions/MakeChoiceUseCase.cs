using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

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

        // Record the choice
        var sessionChoice = new SessionChoice
        {
            SceneId = request.SceneId,
            SceneTitle = currentScene.Title,
            ChoiceText = request.ChoiceText,
            NextScene = request.NextSceneId,
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

        // Check if session is complete (reached end scene or no more branches)
        var nextScene = scenario.Scenes.FirstOrDefault(s => s.Id == request.NextSceneId);
        if (nextScene == null || (!nextScene.Branches.Any() && string.IsNullOrEmpty(nextScene.NextSceneId)))
        {
            session.Status = SessionStatus.Completed;
            session.EndTime = DateTime.UtcNow;
        }

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Choice made in session {SessionId}: {ChoiceText} -> {NextScene}",
            session.Id, request.ChoiceText, request.NextSceneId);

        return session;
    }
}

