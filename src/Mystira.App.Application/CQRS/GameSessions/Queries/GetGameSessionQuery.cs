using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Query to retrieve a game session by ID
/// </summary>
public record GetGameSessionQuery(string SessionId) : IQuery<GameSession?>;
