using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Wolverine handler for GetGameSessionQuery.
/// Retrieves a single game session by ID.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetGameSessionQueryHandler
{
    /// <summary>
    /// Handles the GetGameSessionQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession?> Handle(
        GetGameSessionQuery request,
        IGameSessionRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(request.SessionId);

        if (session == null)
        {
            logger.LogDebug("Session not found: {SessionId}", request.SessionId);
        }
        else
        {
            logger.LogDebug("Retrieved session {SessionId}", request.SessionId);
        }

        return session;
    }
}
