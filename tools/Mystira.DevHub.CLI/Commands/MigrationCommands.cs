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
                return CommandResponse.Fail("Type is required (scenarios, bundles, media-metadata, blobs, all)");
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

            var results = new List<MigrationResult>();

            switch (args.Type.ToLower())
            {
                case "scenarios":
                    if (string.IsNullOrEmpty(sourceCosmosConnection) || string.IsNullOrEmpty(destCosmosConnection))
                    {
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    }
                    var scenarioResult = await _migrationService.MigrateScenariosAsync(sourceCosmosConnection, destCosmosConnection, args.DatabaseName);
                    results.Add(scenarioResult);
                    break;

                case "bundles":
                    if (string.IsNullOrEmpty(sourceCosmosConnection) || string.IsNullOrEmpty(destCosmosConnection))
                    {
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    }
                    var bundleResult = await _migrationService.MigrateContentBundlesAsync(sourceCosmosConnection, destCosmosConnection, args.DatabaseName);
                    results.Add(bundleResult);
                    break;

                case "media-metadata":
                    if (string.IsNullOrEmpty(sourceCosmosConnection) || string.IsNullOrEmpty(destCosmosConnection))
                    {
                        return CommandResponse.Fail("Source and destination Cosmos DB connection strings are required");
                    }
                    var mediaResult = await _migrationService.MigrateMediaAssetsAsync(sourceCosmosConnection, destCosmosConnection, args.DatabaseName);
                    results.Add(mediaResult);
                    break;

                case "blobs":
                    if (string.IsNullOrEmpty(sourceStorageConnection) || string.IsNullOrEmpty(destStorageConnection))
                    {
                        return CommandResponse.Fail("Source and destination storage connection strings are required");
                    }
                    var blobResult = await _migrationService.MigrateBlobStorageAsync(sourceStorageConnection, destStorageConnection, args.ContainerName);
                    results.Add(blobResult);
                    break;

                case "all":
                    // Migrate all Cosmos DB data
                    if (!string.IsNullOrEmpty(sourceCosmosConnection) && !string.IsNullOrEmpty(destCosmosConnection))
                    {
                        results.Add(await _migrationService.MigrateScenariosAsync(sourceCosmosConnection, destCosmosConnection, args.DatabaseName));
                        results.Add(await _migrationService.MigrateContentBundlesAsync(sourceCosmosConnection, destCosmosConnection, args.DatabaseName));
                        results.Add(await _migrationService.MigrateMediaAssetsAsync(sourceCosmosConnection, destCosmosConnection, args.DatabaseName));
                    }

                    // Migrate Blob Storage
                    if (!string.IsNullOrEmpty(sourceStorageConnection) && !string.IsNullOrEmpty(destStorageConnection))
                    {
                        results.Add(await _migrationService.MigrateBlobStorageAsync(sourceStorageConnection, destStorageConnection, args.ContainerName));
                    }
                    break;

                default:
                    return CommandResponse.Fail($"Unknown migration type: {args.Type}");
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
}
