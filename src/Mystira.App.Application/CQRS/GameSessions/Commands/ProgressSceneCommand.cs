using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Command to progress a game session to a new scene
/// </summary>
public record ProgressSceneCommand(ProgressSceneRequest Request) : ICommand<GameSession?>;
