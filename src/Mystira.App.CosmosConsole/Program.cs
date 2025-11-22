using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.CosmosConsole.Data;
using Mystira.App.CosmosConsole.Extensions;
using Mystira.App.CosmosConsole.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.CosmosConsole;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Add DbContext
        var connectionString = configuration.GetConnectionString("CosmosDb");
        var databaseName = configuration["Database:Name"] ?? "MystiraAppDb";

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: Cosmos DB connection string not found in configuration.");
            Console.WriteLine("Please set the ConnectionStrings:CosmosDb in appsettings.json or environment variables.");
            return 1;
        }

        services.AddDbContext<CosmosConsoleDbContext>(options =>
            options.UseCosmos(
                connectionString,
                databaseName
            ));


        // Add our services
        services.AddScoped<ICosmosReportingService, CosmosReportingService>();

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Parse command line arguments
            if (args.Length == 0)
            {
                Console.WriteLine("Mystira Cosmos DB Reporting Console");
                Console.WriteLine("Usage:");
                Console.WriteLine("  export --output <file.csv>    Export game sessions to CSV");
                Console.WriteLine("  stats                       Show scenario completion statistics");
                return 0;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "export":
                    if (args.Length < 3 || args[1] != "--output" || string.IsNullOrEmpty(args[2]))
                    {
                        Console.WriteLine("Error: export command requires --output <file.csv> parameter");
                        return 1;
                    }
                    await ExportGameSessionsToCsv(serviceProvider, args[2], logger);
                    break;

                case "stats":
                    await ShowScenarioStatistics(serviceProvider, logger);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Available commands: export, stats");
                    return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while executing the command");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            // Clean up
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static async Task ExportGameSessionsToCsv(IServiceProvider serviceProvider, string outputFile, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting export of game sessions to CSV: {OutputFile}", outputFile);

            var reportingService = serviceProvider.GetRequiredService<ICosmosReportingService>();
            var sessionsWithAccounts = await reportingService.GetGameSessionReportingTable();
            var csv = sessionsWithAccounts.ToCsv();
            await File.WriteAllTextAsync(outputFile, csv);
            logger.LogInformation("Export completed: {OutputFile}", outputFile);
            Console.WriteLine($"Export completed: {outputFile}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting game sessions to CSV");
            Console.WriteLine($"Error exporting to CSV: {ex.Message}");
        }
    }

    private static async Task ShowScenarioStatistics(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            logger.LogInformation("Generating scenario completion statistics");

            var reportingService = serviceProvider.GetRequiredService<ICosmosReportingService>();
            var statistics = await reportingService.GetScenarioStatisticsAsync();

            if (!statistics.Any())
            {
                Console.WriteLine("No scenario statistics found.");
                return;
            }

            Console.WriteLine("\nScenario Completion Statistics:");
            Console.WriteLine("================================");

            foreach (var stat in statistics.OrderByDescending(s => s.TotalSessions))
            {
                var completionRate = stat.TotalSessions > 0
                    ? (stat.CompletedSessions / (double)stat.TotalSessions) * 100
                    : 0;

                Console.WriteLine($"\nScenario: {stat.ScenarioName}");
                Console.WriteLine($"  Total Sessions: {stat.TotalSessions}");
                Console.WriteLine($"  Completed Sessions: {stat.CompletedSessions}");
                Console.WriteLine($"  Completion Rate: {completionRate:F1}%");
                Console.WriteLine("  Account Breakdown:");

                foreach (var accountStat in stat.AccountStatistics.OrderByDescending(a => a.SessionCount))
                {
                    var accountCompletionRate = accountStat.SessionCount > 0
                        ? (accountStat.CompletedSessions / (double)accountStat.SessionCount) * 100
                        : 0;

                    Console.WriteLine($"    {accountStat.AccountEmail} ({accountStat.AccountAlias}):");
                    Console.WriteLine($"      Sessions: {accountStat.SessionCount}");
                    Console.WriteLine($"      Completed: {accountStat.CompletedSessions}");
                    Console.WriteLine($"      Completion Rate: {accountCompletionRate:F1}%");
                }
            }

            Console.WriteLine("\n================================");
            logger.LogInformation("Statistics generated for {Count} scenarios", statistics.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating scenario statistics");
            Console.WriteLine($"Error generating statistics: {ex.Message}");
        }
    }

    private static string GetScenarioName(string scenarioId)
    {
        // This is a placeholder - in a real implementation, 
        // you might want to fetch scenario data or cache scenario names
        return $"Scenario {scenarioId}";
    }
}
