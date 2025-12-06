using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Command to pause an active game session
/// </summary>
public record PauseGameSessionCommand(string SessionId) : ICommand<GameSession?>;
