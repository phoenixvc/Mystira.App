using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Command to resume a paused game session
/// </summary>
public record ResumeGameSessionCommand(string SessionId) : ICommand<GameSession?>;
