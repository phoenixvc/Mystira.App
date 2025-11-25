using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification to filter sessions by account ID
/// </summary>
public class SessionsByAccountSpecification : BaseSpecification<GameSession>
{
    public SessionsByAccountSpecification(string accountId)
        : base(s => s.AccountId == accountId)
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}

/// <summary>
/// Specification to filter sessions by profile ID
/// </summary>
public class SessionsByProfileSpecification : BaseSpecification<GameSession>
{
    public SessionsByProfileSpecification(string profileId)
        : base(s => s.ProfileId == profileId)
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}

/// <summary>
/// Specification to filter in-progress and paused sessions for an account
/// </summary>
public class InProgressSessionsSpecification : BaseSpecification<GameSession>
{
    public InProgressSessionsSpecification(string accountId)
        : base(s => s.AccountId == accountId &&
                   (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused))
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}

/// <summary>
/// Specification to filter sessions by scenario ID
/// </summary>
public class SessionsByScenarioSpecification : BaseSpecification<GameSession>
{
    public SessionsByScenarioSpecification(string scenarioId)
        : base(s => s.ScenarioId == scenarioId)
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}

/// <summary>
/// Specification to filter active sessions (in progress or paused)
/// </summary>
public class ActiveSessionsSpecification : BaseSpecification<GameSession>
{
    public ActiveSessionsSpecification()
        : base(s => s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused)
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}

/// <summary>
/// Specification to filter completed sessions
/// </summary>
public class CompletedSessionsSpecification : BaseSpecification<GameSession>
{
    public CompletedSessionsSpecification()
        : base(s => s.Status == SessionStatus.Completed)
    {
        ApplyOrderByDescending(s => s.EndTime ?? s.StartTime);
    }
}

/// <summary>
/// Specification to filter sessions by status
/// </summary>
public class SessionsByStatusSpecification : BaseSpecification<GameSession>
{
    public SessionsByStatusSpecification(SessionStatus status)
        : base(s => s.Status == status)
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}

/// <summary>
/// Specification to filter sessions by account and scenario
/// </summary>
public class SessionsByAccountAndScenarioSpecification : BaseSpecification<GameSession>
{
    public SessionsByAccountAndScenarioSpecification(string accountId, string scenarioId)
        : base(s => s.AccountId == accountId && s.ScenarioId == scenarioId)
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}
