using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.CosmosConsole.Data;
using Mystira.App.Domain.Models;

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

    public async Task<DataTable> GetGameSessionReportingTable()
    {
        try
        {
            _logger.LogInformation("Loading game sessions with account information");

            var sessions = await _context.GameSessions.ToListAsync();
            var scenarios = await _context.Scenarios.ToListAsync();
            var accounts = await _context.Accounts.ToListAsync();

            // Create a dictionary for quick lookup of accounts by Id
            var accountsDict = accounts.ToDictionary(a => a.Id);

            // Create a dictionary for quick lookup of scenarios by Id
            var scenariosDict = scenarios.ToDictionary(s => s.Id);

            // Join sessions with accounts and scenarios
            var sessionList = sessions.Select<GameSession, SessionTableRow>(session =>
            {
                // Look up the account and scenario for this session
                accountsDict.TryGetValue(session.AccountId, out var account);
                scenariosDict.TryGetValue(session.ScenarioId, out var scenario);

                return new SessionTableRow
                {
                    SessionId = session.Id,
                    StartedUtc = session.StartTime,
                    CompletedUtc = session.EndTime,
                    Duration = session.EndTime.HasValue ? (session.EndTime.Value - session.StartTime).ToString(@"hh\:mm\:ss") : "",
                    Status = session.EndTime.HasValue ? "Completed" : "In Progress",

                    // Account information
                    AccountId = session.AccountId,
                    AccountDisplayName = account?.DisplayName ?? "",
                    AccountEmail = account?.Email ?? "",

                    // Scenario information
                    ScenarioId = session.ScenarioId,
                    ScenarioName = scenario?.Title ?? "Unknown Scenario",

                    // Other session information
                    PlayerNames = string.Join(",", session.PlayerNames)
                };
            }).ToList();

            // Convert the list to a DataTable
            DataTable dataTable = new DataTable("GameSessions");

            // Add columns to the DataTable
            dataTable.Columns.Add("SessionId", typeof(string));
            dataTable.Columns.Add("StartedUtc", typeof(DateTime));
            dataTable.Columns.Add("CompletedUtc", typeof(DateTime));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("Duration", typeof(string));
            dataTable.Columns.Add("AccountId", typeof(string));
            dataTable.Columns.Add("AccountDisplayName", typeof(string));
            dataTable.Columns.Add("AccountEmail", typeof(string));
            dataTable.Columns.Add("ScenarioId", typeof(string));
            dataTable.Columns.Add("ScenarioName", typeof(string));
            dataTable.Columns.Add("PlayerNames", typeof(string));

            // Add rows to the DataTable
            foreach (var item in sessionList)
            {
                DataRow row = dataTable.NewRow();

                row["SessionId"] = item.SessionId;
                row["StartedUtc"] = item.StartedUtc;
                row["CompletedUtc"] = item.CompletedUtc.HasValue ? item.CompletedUtc.Value : DBNull.Value;
                row["Status"] = item.Status;
                row["AccountId"] = item.AccountId;
                row["AccountDisplayName"] = item.AccountDisplayName;
                row["AccountEmail"] = item.AccountEmail;
                row["ScenarioId"] = item.ScenarioId;
                row["ScenarioName"] = item.ScenarioName;
                row["PlayerNames"] = item.PlayerNames;

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game sessions with accounts");
            throw;
        }
    }

    private class SessionTableRow
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string AccountDisplayName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public string ScenarioId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public string PlayerNames { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
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
