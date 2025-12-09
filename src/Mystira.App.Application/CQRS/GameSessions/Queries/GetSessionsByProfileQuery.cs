using Mystira.App.Contracts.Responses.GameSessions;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Query to retrieve all game sessions for a specific profile
/// </summary>
public record GetSessionsByProfileQuery(string ProfileId) : IQuery<List<GameSessionResponse>>;
