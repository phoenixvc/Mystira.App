using Mystira.App.Contracts.Responses.GameSessions;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Query to retrieve in-progress and paused sessions for an account
/// </summary>
public record GetInProgressSessionsQuery(string AccountId) : IQuery<List<GameSessionResponse>>;
