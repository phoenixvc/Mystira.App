using Mystira.Contracts.App.Requests.GameSessions;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Command to start a new game session
/// </summary>
public record StartGameSessionCommand(StartGameSessionRequest Request) : ICommand<GameSession>;
