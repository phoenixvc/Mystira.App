using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.CosmosConsole.Data;

namespace Mystira.App.CosmosConsole.Services;

public class CosmosReportingService : ICosmosReportingService
{
    private readonly CosmosConsoleDbContext _context;
    private readonly ILogger<CosmosReportingService> _logger;

    public CosmosReportingService(CosmosConsoleDbContext context, ILogger<CosmosReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GameSessionWithAccount>> GetGameSessionsWithAccountsAsync()
    {
        try
        {
            _logger.LogInformation("Loading game sessions with account information");

            var sessions = await _context.GameSessions.ToListAsync();
            var scenarios = await _context.Scenarios.ToListAsync();
            var accounts = await _context.Accounts.ToListAsync();

            var result = sessions
                .Select(session => new GameSessionWithAccount
                {
                    Session = session,
                    Account = accounts.FirstOrDefault(a => a.Id == session.AccountId)
                })
                .Where(x => x.Account != null)
                .Select(x => new GameSessionWithAccount
                {
                    Session = new GameSession
                    {
                        Id = x.Session.Id,
                        ScenarioId = x.Session.ScenarioId,
                        AccountId = x.Session.AccountId,
                        ProfileId = x.Session.ProfileId,
                        Status = x.Session.Status,
                        StartTime = x.Session.StartTime,
                        EndTime = x.Session.EndTime,
                        ElapsedTime = x.Session.ElapsedTime,
                        IsPaused = x.Session.IsPaused,
                        PausedAt = x.Session.PausedAt,
                        SceneCount = x.Session.SceneCount,
                        TargetAgeGroupName = x.Session.TargetAgeGroupName,
                        SelectedCharacterId = x.Session.SelectedCharacterId
                        // Copy navigation property manually
                    },
                    Account = x.Account
                })
                .OrderByDescending(x => x.Session.StartTime)
                .ToList();

            _logger.LogInformation("Loaded {Count} game sessions with account data", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game sessions with accounts");
            throw;
        }
    }

    public async Task<List<ScenarioStatistics>> GetScenarioStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Generating scenario completion statistics");

            // Get all game sessions and scenarios
            var sessions = await _context.GameSessions.ToListAsync();
            var scenarios = await _context.Scenarios.ToListAsync();
            var accounts = await _context.Accounts.ToListAsync();

            // Group sessions by scenario
            var scenarioGroups = sessions
                .GroupBy(s => s.ScenarioId)
                .ToList();

            var statistics = new List<ScenarioStatistics>();

            foreach (var group in scenarioGroups)
            {
                var scenario = scenarios.FirstOrDefault(sc => sc.Id == group.Key);
                var scenarioName = scenario?.Title ?? "Unknown Scenario";
                
                var scenarioStat = new ScenarioStatistics
                {
                    ScenarioId = group.Key,
                    ScenarioName = scenarioName,
                    TotalSessions = group.Count(),
                    CompletedSessions = group.Count(s => s.Status == SessionStatus.Completed)
                };

                // Get account breakdown for this scenario
                var accountGroups = group
                    .GroupBy(s => s.AccountId)
                    .ToList();

                foreach (var accountGroup in accountGroups)
                {
                    var account = accounts.FirstOrDefault(a => a.Id == accountGroup.Key);
                    if (account != null)
                    {
                        scenarioStat.AccountStatistics.Add(new AccountScenarioStatistics
                        {
                            AccountId = account.Id,
                            AccountEmail = account.Email,
                            AccountAlias = account.DisplayName,
                            SessionCount = accountGroup.Count(),
                            CompletedSessions = accountGroup.Count(s => s.Status == SessionStatus.Completed)
                        });
                    }
                }

                statistics.Add(scenarioStat);
            }

            _logger.LogInformation("Generated statistics for {Count} scenarios", statistics.Count);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating scenario statistics");
            throw;
        }
    }
}