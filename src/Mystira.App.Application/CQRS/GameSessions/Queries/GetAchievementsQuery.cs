using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Query to retrieve achievements for a game session
/// </summary>
public record GetAchievementsQuery(string SessionId) : IQuery<List<SessionAchievement>>;
