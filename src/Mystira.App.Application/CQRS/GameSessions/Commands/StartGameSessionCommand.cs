using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Command to start a new game session
/// </summary>
public record StartGameSessionCommand(StartGameSessionRequest Request) : ICommand<GameSession>;
