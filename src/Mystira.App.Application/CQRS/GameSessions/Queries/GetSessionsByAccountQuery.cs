using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Responses.GameSessions;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Query to retrieve all game sessions for a specific account
/// </summary>
public record GetSessionsByAccountQuery(string AccountId) : IQuery<List<GameSessionResponse>>;
