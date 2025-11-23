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
        services.AddScoped<IMigrationService, MigrationService>();

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Parse command line arguments
            if (args.Length == 0)
            {
                ShowHelp();
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

                case "migrate":
                    if (args.Length < 2)
                    {
                        ShowMigrateHelp();
                        return 1;
                    }
                    return await ExecuteMigration(serviceProvider, args, logger, configuration);

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Available commands: export, stats, migrate");
                    Console.WriteLine("Use 'migrate --help' for migration options");
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

    private static void ShowHelp()
    {
        Console.WriteLine("Mystira Cosmos DB Management Console");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  export --output <file.csv>     Export game sessions to CSV");
        Console.WriteLine("  stats                          Show scenario completion statistics");
        Console.WriteLine("  migrate <type> [options]       Migrate data between environments");
        Console.WriteLine("\nMigration Commands:");
        Console.WriteLine("  migrate --help                 Show detailed migration help");
        Console.WriteLine("  migrate scenarios              Migrate scenarios");
        Console.WriteLine("  migrate bundles                Migrate content bundles");
        Console.WriteLine("  migrate media-metadata         Migrate media asset metadata");
        Console.WriteLine("  migrate blobs                  Migrate blob storage files");
        Console.WriteLine("  migrate all                    Migrate everything");
    }

    private static void ShowMigrateHelp()
    {
        Console.WriteLine("Mystira Data Migration Tool");
        Console.WriteLine("\nMigration requires setting up source and destination connection strings:");
        Console.WriteLine("\nFor Cosmos DB migrations:");
        Console.WriteLine("  Set environment variables:");
        Console.WriteLine("    SOURCE_COSMOS_CONNECTION     Source Cosmos DB connection string");
        Console.WriteLine("    DEST_COSMOS_CONNECTION       Destination Cosmos DB connection string");
        Console.WriteLine("    COSMOS_DATABASE_NAME         Database name (default: MystiraAppDb)");
        Console.WriteLine("\nFor Blob Storage migration:");
        Console.WriteLine("  Set environment variables:");
        Console.WriteLine("    SOURCE_STORAGE_CONNECTION    Source storage connection string");
        Console.WriteLine("    DEST_STORAGE_CONNECTION      Destination storage connection string");
        Console.WriteLine("    STORAGE_CONTAINER_NAME       Container name (default: mystira-app-media)");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  # Migrate scenarios");
        Console.WriteLine("  dotnet run -- migrate scenarios");
        Console.WriteLine("");
        Console.WriteLine("  # Migrate content bundles");
        Console.WriteLine("  dotnet run -- migrate bundles");
        Console.WriteLine("");
        Console.WriteLine("  # Migrate media assets metadata");
        Console.WriteLine("  dotnet run -- migrate media-metadata");
        Console.WriteLine("");
        Console.WriteLine("  # Migrate blob storage files");
        Console.WriteLine("  dotnet run -- migrate blobs");
        Console.WriteLine("");
        Console.WriteLine("  # Migrate everything");
        Console.WriteLine("  dotnet run -- migrate all");
        Console.WriteLine("\nNote: Old resource names (example):");
        Console.WriteLine("  Cosmos: mystiraappdevcosmos");
        Console.WriteLine("  Storage: mystiraappdevstorage");
        Console.WriteLine("\nNew resource names (standardized):");
        Console.WriteLine("  Cosmos: dev-euw-cosmos-mystira");
        Console.WriteLine("  Storage: deveuwstmystira");
    }

    private static async Task<int> ExecuteMigration(IServiceProvider serviceProvider, string[] args, ILogger logger, IConfiguration configuration)
    {
        if (args[1] == "--help")
        {
            ShowMigrateHelp();
            return 0;
        }

        var migrationType = args[1].ToLower();
        var migrationService = serviceProvider.GetRequiredService<IMigrationService>();

        // Get connection strings from environment or configuration
        var sourceCosmosConnection = Environment.GetEnvironmentVariable("SOURCE_COSMOS_CONNECTION") 
            ?? configuration.GetConnectionString("SourceCosmosDb") ?? "";
        var destCosmosConnection = Environment.GetEnvironmentVariable("DEST_COSMOS_CONNECTION") 
            ?? configuration.GetConnectionString("DestCosmosDb") ?? configuration.GetConnectionString("CosmosDb") ?? "";
        var databaseName = Environment.GetEnvironmentVariable("COSMOS_DATABASE_NAME") 
            ?? configuration["Database:Name"] ?? "MystiraAppDb";

        var sourceStorageConnection = Environment.GetEnvironmentVariable("SOURCE_STORAGE_CONNECTION") 
            ?? configuration.GetConnectionString("SourceStorage") ?? "";
        var destStorageConnection = Environment.GetEnvironmentVariable("DEST_STORAGE_CONNECTION") 
            ?? configuration.GetConnectionString("DestStorage") ?? configuration.GetConnectionString("AzureStorage") ?? "";
        var containerName = Environment.GetEnvironmentVariable("STORAGE_CONTAINER_NAME") 
            ?? configuration["Storage:ContainerName"] ?? "mystira-app-media";

        try
        {
            switch (migrationType)
            {
                case "scenarios":
                    if (string.IsNullOrEmpty(sourceCosmosConnection) || string.IsNullOrEmpty(destCosmosConnection))
                    {
                        Console.WriteLine("Error: Source and destination Cosmos DB connection strings are required");
                        Console.WriteLine("Set SOURCE_COSMOS_CONNECTION and DEST_COSMOS_CONNECTION environment variables");
                        return 1;
                    }
                    Console.WriteLine("Migrating scenarios...");
                    var scenarioResult = await migrationService.MigrateScenariosAsync(sourceCosmosConnection, destCosmosConnection, databaseName);
                    PrintMigrationResult("Scenarios", scenarioResult);
                    return scenarioResult.Success ? 0 : 1;

                case "bundles":
                    if (string.IsNullOrEmpty(sourceCosmosConnection) || string.IsNullOrEmpty(destCosmosConnection))
                    {
                        Console.WriteLine("Error: Source and destination Cosmos DB connection strings are required");
                        return 1;
                    }
                    Console.WriteLine("Migrating content bundles...");
                    var bundleResult = await migrationService.MigrateContentBundlesAsync(sourceCosmosConnection, destCosmosConnection, databaseName);
                    PrintMigrationResult("Content Bundles", bundleResult);
                    return bundleResult.Success ? 0 : 1;

                case "media-metadata":
                    if (string.IsNullOrEmpty(sourceCosmosConnection) || string.IsNullOrEmpty(destCosmosConnection))
                    {
                        Console.WriteLine("Error: Source and destination Cosmos DB connection strings are required");
                        return 1;
                    }
                    Console.WriteLine("Migrating media assets metadata...");
                    var mediaResult = await migrationService.MigrateMediaAssetsAsync(sourceCosmosConnection, destCosmosConnection, databaseName);
                    PrintMigrationResult("Media Assets Metadata", mediaResult);
                    return mediaResult.Success ? 0 : 1;

                case "blobs":
                    if (string.IsNullOrEmpty(sourceStorageConnection) || string.IsNullOrEmpty(destStorageConnection))
                    {
                        Console.WriteLine("Error: Source and destination storage connection strings are required");
                        Console.WriteLine("Set SOURCE_STORAGE_CONNECTION and DEST_STORAGE_CONNECTION environment variables");
                        return 1;
                    }
                    Console.WriteLine($"Migrating blob storage (container: {containerName})...");
                    var blobResult = await migrationService.MigrateBlobStorageAsync(sourceStorageConnection, destStorageConnection, containerName);
                    PrintMigrationResult("Blob Storage", blobResult);
                    return blobResult.Success ? 0 : 1;

                case "all":
                    Console.WriteLine("Starting complete migration...");
                    Console.WriteLine("================================\n");
                    
                    var allSuccess = true;
                    
                    // Migrate Cosmos DB data
                    if (!string.IsNullOrEmpty(sourceCosmosConnection) && !string.IsNullOrEmpty(destCosmosConnection))
                    {
                        Console.WriteLine("1. Migrating scenarios...");
                        var r1 = await migrationService.MigrateScenariosAsync(sourceCosmosConnection, destCosmosConnection, databaseName);
                        PrintMigrationResult("Scenarios", r1);
                        allSuccess &= r1.Success;

                        Console.WriteLine("\n2. Migrating content bundles...");
                        var r2 = await migrationService.MigrateContentBundlesAsync(sourceCosmosConnection, destCosmosConnection, databaseName);
                        PrintMigrationResult("Content Bundles", r2);
                        allSuccess &= r2.Success;

                        Console.WriteLine("\n3. Migrating media assets metadata...");
                        var r3 = await migrationService.MigrateMediaAssetsAsync(sourceCosmosConnection, destCosmosConnection, databaseName);
                        PrintMigrationResult("Media Assets Metadata", r3);
                        allSuccess &= r3.Success;
                    }
                    else
                    {
                        Console.WriteLine("Skipping Cosmos DB migrations (connection strings not configured)");
                    }

                    // Migrate Blob Storage
                    if (!string.IsNullOrEmpty(sourceStorageConnection) && !string.IsNullOrEmpty(destStorageConnection))
                    {
                        Console.WriteLine($"\n4. Migrating blob storage (container: {containerName})...");
                        var r4 = await migrationService.MigrateBlobStorageAsync(sourceStorageConnection, destStorageConnection, containerName);
                        PrintMigrationResult("Blob Storage", r4);
                        allSuccess &= r4.Success;
                    }
                    else
                    {
                        Console.WriteLine("Skipping blob storage migration (connection strings not configured)");
                    }

                    Console.WriteLine("\n================================");
                    Console.WriteLine(allSuccess ? "All migrations completed successfully!" : "Some migrations failed. Check logs above.");
                    return allSuccess ? 0 : 1;

                default:
                    Console.WriteLine($"Unknown migration type: {migrationType}");
                    Console.WriteLine("Available types: scenarios, bundles, media-metadata, blobs, all");
                    Console.WriteLine("Use 'migrate --help' for more information");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed with error");
            Console.WriteLine($"Migration failed: {ex.Message}");
            return 1;
        }
    }

    private static void PrintMigrationResult(string name, MigrationResult result)
    {
        Console.WriteLine($"\n{name} Migration Result:");
        Console.WriteLine($"  Total Items: {result.TotalItems}");
        Console.WriteLine($"  Successful: {result.SuccessCount}");
        Console.WriteLine($"  Failed: {result.FailureCount}");
        Console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"  Status: {(result.Success ? "✓ SUCCESS" : "✗ FAILED")}");
        
        if (result.Errors.Count > 0)
        {
            Console.WriteLine($"\n  Errors ({result.Errors.Count}):");
            foreach (var error in result.Errors.Take(10))
            {
                Console.WriteLine($"    - {error}");
            }
            if (result.Errors.Count > 10)
            {
                Console.WriteLine($"    ... and {result.Errors.Count - 10} more errors");
            }
        }
    }
}
