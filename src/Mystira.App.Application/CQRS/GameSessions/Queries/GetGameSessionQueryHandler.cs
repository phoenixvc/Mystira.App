using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Handler for GetGameSessionQuery
/// Retrieves a single game session by ID
/// </summary>
public class GetGameSessionQueryHandler : IQueryHandler<GetGameSessionQuery, GameSession?>
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetGameSessionQueryHandler> _logger;

    public GetGameSessionQueryHandler(
        IGameSessionRepository repository,
        ILogger<GetGameSessionQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GameSession?> Handle(
        GetGameSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId);

        if (session == null)
        {
            _logger.LogDebug("Session not found: {SessionId}", request.SessionId);
        }
        else
        {
            _logger.LogDebug("Retrieved session {SessionId}", request.SessionId);
        }

        return session;
    }
}
