using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Mystira.DevHub.CLI.Models;
using Mystira.DevHub.Services.Migration;

namespace Mystira.DevHub.CLI.Commands;

public class MigrationCommands
{
    private readonly IMigrationService _migrationService;
    private readonly IConfiguration _configuration;

    public MigrationCommands(IMigrationService migrationService, IConfiguration configuration)
    {
        _migrationService = migrationService;
        _configuration = configuration;
    }

    public async Task<CommandResponse> RunAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<MigrationArgs>(argsJson.GetRawText());
            if (args == null || string.IsNullOrEmpty(args.Type))
            {
                return CommandResponse.Fail("Type is required (scenarios, bundles, media-metadata, blobs, master-data, all)");
            }

            // Get connection strings from args, environment, or configuration
            var sourceCosmosConnection = args.SourceCosmosConnection
                ?? Environment.GetEnvironmentVariable("SOURCE_COSMOS_CONNECTION")
                ?? _configuration.GetConnectionString("SourceCosmosDb")
                ?? "";

            var destCosmosConnection = args.DestCosmosConnection
                ?? Environment.GetEnvironmentVariable("DEST_COSMOS_CONNECTION")
                ?? _configuration.GetConnectionString("DestCosmosDb")
                ?? _configuration.GetConnectionString("CosmosDb")
                ?? "";

            var sourceStorageConnection = args.SourceStorageConnection
                ?? Environment.GetEnvironmentVariable("SOURCE_STORAGE_CONNECTION")
                ?? _configuration.GetConnectionString("SourceStorage")
                ?? "";

            var destStorageConnection = args.DestStorageConnection
                ?? Environment.GetEnvironmentVariable("DEST_STORAGE_CONNECTION")
                ?? _configuration.GetConnectionString("DestStorage")
                ?? _configuration.GetConnectionString("AzureStorage")
                ?? "";

            var jsonFilesPath = args.JsonFilesPath
                ?? Environment.GetEnvironmentVariable("MASTER_DATA_JSON_PATH")
                ?? FindMasterDataJsonPath();

            var results = new List<MigrationResult>();

            // Helper to validate cosmos connections
            bool HasCosmosConnections() => !string.IsNullOrEmpty(sourceCosmosConnection) && !string.IsNullOrEmpty(destCosmosConnection);

            switch (args.Type.ToLower())
            {
                case "scenarios":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateScenariosAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName));
                    break;

                case "bundles":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContentBundlesAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName));
                    break;

                case "media-metadata":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateMediaAssetsAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName));
                    break;

                // User data containers - using generic migration
                case "user-profiles":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "UserProfiles", "/id", args.DryRun));
                    break;

                case "game-sessions":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "GameSessions", "/id", args.DryRun));
                    break;

                case "accounts":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "Accounts", "/id", args.DryRun));
                    break;

                case "compass-trackings":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CompassTrackings", "/id", args.DryRun));
                    break;

                // Reference data containers - using generic migration
                case "character-maps":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CharacterMaps", "/id", args.DryRun));
                    break;

                case "character-map-files":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CharacterMapFiles", "/id", args.DryRun));
                    break;

                case "character-media-metadata-files":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CharacterMediaMetadataFiles", "/id", args.DryRun));
                    break;

                case "avatar-configuration-files":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "AvatarConfigurationFiles", "/id", args.DryRun));
                    break;

                case "badge-configurations":
                    if (!HasCosmosConnections())
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "BadgeConfigurations", "/id", args.DryRun));
                    break;

                case "blobs":
                    if (string.IsNullOrEmpty(sourceStorageConnection) || string.IsNullOrEmpty(destStorageConnection))
                        return CommandResponse.Fail("Source and destination storage connection strings are required");
                    results.Add(await _migrationService.MigrateBlobStorageAsync(sourceStorageConnection, destStorageConnection, args.ContainerName));
                    break;

                case "master-data":
                    if (string.IsNullOrEmpty(destCosmosConnection))
                        return CommandResponse.Fail("Destination Cosmos DB connection string is required");
                    results.Add(await _migrationService.SeedMasterDataAsync(destCosmosConnection, args.DestDatabaseName, jsonFilesPath));
                    break;

                case "all":
                    // Migrate all Cosmos DB containers
                    if (HasCosmosConnections())
                    {
                        // Core content
                        results.Add(await _migrationService.MigrateScenariosAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName));
                        results.Add(await _migrationService.MigrateContentBundlesAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName));
                        results.Add(await _migrationService.MigrateMediaAssetsAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName));

                        // User data
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "UserProfiles", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "GameSessions", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "Accounts", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CompassTrackings", "/id", args.DryRun));

                        // Reference data
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CharacterMaps", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CharacterMapFiles", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "CharacterMediaMetadataFiles", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "AvatarConfigurationFiles", "/id", args.DryRun));
                        results.Add(await _migrationService.MigrateContainerAsync(sourceCosmosConnection, destCosmosConnection, args.SourceDatabaseName, args.DestDatabaseName, "BadgeConfigurations", "/id", args.DryRun));
                    }

                    // Seed master data
                    if (!string.IsNullOrEmpty(destCosmosConnection))
                    {
                        results.Add(await _migrationService.SeedMasterDataAsync(destCosmosConnection, args.DestDatabaseName, jsonFilesPath));
                    }

                    // Migrate Blob Storage
                    if (!string.IsNullOrEmpty(sourceStorageConnection) && !string.IsNullOrEmpty(destStorageConnection))
                    {
                        results.Add(await _migrationService.MigrateBlobStorageAsync(sourceStorageConnection, destStorageConnection, args.ContainerName));
                    }
                    break;

                default:
                    return CommandResponse.Fail($"Unknown migration type: {args.Type}. Valid types: scenarios, bundles, media-metadata, user-profiles, game-sessions, accounts, compass-trackings, character-maps, character-map-files, character-media-metadata-files, avatar-configuration-files, badge-configurations, blobs, master-data, all");
            }

            var overallSuccess = results.All(r => r.Success);
            var totalItems = results.Sum(r => r.TotalItems);
            var totalSuccess = results.Sum(r => r.SuccessCount);
            var totalFailures = results.Sum(r => r.FailureCount);

            return CommandResponse.Ok(new
            {
                overallSuccess,
                totalItems,
                totalSuccess,
                totalFailures,
                results
            }, $"Migration completed: {totalSuccess}/{totalItems} successful");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Searches for the master data JSON files directory by looking for known files.
    /// </summary>
    private static string FindMasterDataJsonPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Search for the Data directory containing CoreAxes.json
        var searchPaths = new[]
        {
            // When running from tools/Mystira.DevHub.CLI/bin
            Path.Combine(baseDir, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Data"),
            // When running from solution root
            Path.Combine(baseDir, "src", "Mystira.App.Domain", "Data"),
            // When running from src directory
            Path.Combine(baseDir, "Mystira.App.Domain", "Data"),
            // Relative to current directory
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Mystira.App.Domain", "Data"),
        };

        foreach (var path in searchPaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "CoreAxes.json")))
            {
                return fullPath;
            }
        }

        // Fallback to most likely path
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Data"));
    }
}
