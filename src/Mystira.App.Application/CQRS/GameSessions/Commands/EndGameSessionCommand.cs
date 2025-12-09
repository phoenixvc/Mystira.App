using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Command to end an active game session
/// </summary>
public record EndGameSessionCommand(string SessionId) : ICommand<GameSession?>;
