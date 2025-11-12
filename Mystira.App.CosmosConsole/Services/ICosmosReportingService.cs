using Mystira.App.Domain.Models;

namespace Mystira.App.CosmosConsole.Services;

public interface ICosmosReportingService
{
    Task<List<GameSessionWithAccount>> GetGameSessionsWithAccountsAsync();
    Task<List<ScenarioStatistics>> GetScenarioStatisticsAsync();
}

public class GameSessionWithAccount
{
    public GameSession Session { get; set; } = new();
    public Account? Account { get; set; }
}

public class ScenarioStatistics
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public List<AccountScenarioStatistics> AccountStatistics { get; set; } = new();
}

public class AccountScenarioStatistics
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountEmail { get; set; } = string.Empty;
    public string AccountAlias { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int CompletedSessions { get; set; }
}