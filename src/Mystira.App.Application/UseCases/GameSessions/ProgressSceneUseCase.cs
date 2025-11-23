using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for progressing to a specific scene in a game session
/// </summary>
public class ProgressSceneUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProgressSceneUseCase> _logger;

    public ProgressSceneUseCase(
        IGameSessionRepository repository,
        IScenarioRepository scenarioRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProgressSceneUseCase> logger)
    {
        _repository = repository;
        _scenarioRepository = scenarioRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> ExecuteAsync(ProgressSceneRequest request)
    {
        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            return null;
        }

        if (session.Status != SessionStatus.InProgress && session.Status != SessionStatus.Paused)
        {
            throw new InvalidOperationException($"Cannot progress scene in session with status {session.Status}");
        }

        var scenario = await _scenarioRepository.GetByIdAsync(session.ScenarioId);
        if (scenario == null)
        {
            throw new InvalidOperationException("Scenario not found for session");
        }

        var targetScene = scenario.Scenes.FirstOrDefault(s => s.Id == request.SceneId);
        if (targetScene == null)
        {
            throw new ArgumentException($"Scene {request.SceneId} not found in scenario");
        }

        // Update session state
        session.CurrentSceneId = request.SceneId;
        session.ElapsedTime = DateTime.UtcNow - session.StartTime;

        // Resume if paused
        if (session.Status == SessionStatus.Paused)
        {
            session.Status = SessionStatus.InProgress;
            session.IsPaused = false;
            session.PausedAt = null;
        }

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Progressed session {SessionId} to scene {SceneId}",
            session.Id, request.SceneId);

        return session;
    }
}

