using Mystira.App.Contracts.Responses.GameSessions;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Query to retrieve session statistics and analytics
/// </summary>
public record GetSessionStatsQuery(string SessionId) : IQuery<SessionStatsResponse?>;
