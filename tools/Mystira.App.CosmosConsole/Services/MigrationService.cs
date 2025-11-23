using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using System.Diagnostics;

namespace Mystira.App.CosmosConsole.Services;

public class MigrationService : IMigrationService
{
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(ILogger<MigrationService> logger)
    {
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateScenariosAsync(string sourceConnectionString, string destConnectionString, string databaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting scenarios migration");

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString);

            var sourceContainer = sourceClient.GetContainer(databaseName, "Scenarios");
            var destContainer = destClient.GetContainer(databaseName, "Scenarios");

            // Ensure destination container exists
            await EnsureContainerExists(destClient, databaseName, "Scenarios", "/id");

            // Query all scenarios from source
            var query = sourceContainer.GetItemQueryIterator<Scenario>("SELECT * FROM c");
            var scenarios = new List<Scenario>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                scenarios.AddRange(response);
            }

            result.TotalItems = scenarios.Count;
            _logger.LogInformation("Found {Count} scenarios to migrate", scenarios.Count);

            // Migrate each scenario
            foreach (var scenario in scenarios)
            {
                try
                {
                    await destContainer.UpsertItemAsync(scenario, new PartitionKey(scenario.Id));
                    result.SuccessCount++;
                    _logger.LogDebug("Migrated scenario: {Id} - {Title}", scenario.Id, scenario.Title);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to migrate scenario {scenario.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate scenario {Id}", scenario.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Scenarios migration completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during scenarios migration");
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<MigrationResult> MigrateContentBundlesAsync(string sourceConnectionString, string destConnectionString, string databaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting content bundles migration");

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString);

            var sourceContainer = sourceClient.GetContainer(databaseName, "ContentBundles");
            var destContainer = destClient.GetContainer(databaseName, "ContentBundles");

            // Ensure destination container exists
            await EnsureContainerExists(destClient, databaseName, "ContentBundles", "/id");

            // Query all content bundles from source
            var query = sourceContainer.GetItemQueryIterator<ContentBundle>("SELECT * FROM c");
            var bundles = new List<ContentBundle>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                bundles.AddRange(response);
            }

            result.TotalItems = bundles.Count;
            _logger.LogInformation("Found {Count} content bundles to migrate", bundles.Count);

            // Migrate each bundle
            foreach (var bundle in bundles)
            {
                try
                {
                    await destContainer.UpsertItemAsync(bundle, new PartitionKey(bundle.Id));
                    result.SuccessCount++;
                    _logger.LogDebug("Migrated content bundle: {Id} - {Title}", bundle.Id, bundle.Title);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to migrate content bundle {bundle.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate content bundle {Id}", bundle.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Content bundles migration completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during content bundles migration");
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<MigrationResult> MigrateMediaAssetsAsync(string sourceConnectionString, string destConnectionString, string databaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting media assets migration");

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString);

            var sourceContainer = sourceClient.GetContainer(databaseName, "MediaAssets");
            var destContainer = destClient.GetContainer(databaseName, "MediaAssets");

            // Ensure destination container exists
            await EnsureContainerExists(destClient, databaseName, "MediaAssets", "/id");

            // Query all media assets from source
            var query = sourceContainer.GetItemQueryIterator<MediaAsset>("SELECT * FROM c");
            var assets = new List<MediaAsset>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                assets.AddRange(response);
            }

            result.TotalItems = assets.Count;
            _logger.LogInformation("Found {Count} media assets to migrate", assets.Count);

            // Migrate each asset
            foreach (var asset in assets)
            {
                try
                {
                    await destContainer.UpsertItemAsync(asset, new PartitionKey(asset.Id));
                    result.SuccessCount++;
                    _logger.LogDebug("Migrated media asset: {Id} - {MediaId}", asset.Id, asset.MediaId);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to migrate media asset {asset.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate media asset {Id}", asset.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Media assets migration completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during media assets migration");
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<MigrationResult> MigrateBlobStorageAsync(string sourceStorageConnectionString, string destStorageConnectionString, string containerName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting blob storage migration for container: {Container}", containerName);

            var sourceBlobServiceClient = new BlobServiceClient(sourceStorageConnectionString);
            var destBlobServiceClient = new BlobServiceClient(destStorageConnectionString);

            var sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(containerName);
            var destContainerClient = destBlobServiceClient.GetBlobContainerClient(containerName);

            // Ensure destination container exists
            await destContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // List all blobs in source container
            var blobs = new List<string>();
            await foreach (var blobItem in sourceContainerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }

            result.TotalItems = blobs.Count;
            _logger.LogInformation("Found {Count} blobs to migrate", blobs.Count);

            // Migrate each blob
            foreach (var blobName in blobs)
            {
                try
                {
                    var sourceBlobClient = sourceContainerClient.GetBlobClient(blobName);
                    var destBlobClient = destContainerClient.GetBlobClient(blobName);

                    // Check if blob exists in destination
                    var destExists = await destBlobClient.ExistsAsync();
                    if (destExists.Value)
                    {
                        _logger.LogDebug("Blob {BlobName} already exists in destination, skipping", blobName);
                        result.SuccessCount++;
                        continue;
                    }

                    // Copy blob using server-side copy
                    var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                    await copyOperation.WaitForCompletionAsync();

                    result.SuccessCount++;
                    _logger.LogDebug("Migrated blob: {BlobName}", blobName);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to migrate blob {blobName}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate blob {BlobName}", blobName);
                }
            }

            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Blob storage migration completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during blob storage migration");
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task EnsureContainerExists(CosmosClient client, string databaseName, string containerName, string partitionKeyPath)
    {
        try
        {
            var database = client.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerName, partitionKeyPath);
            _logger.LogInformation("Container {Container} ready (created: {Created})", containerName, containerResponse.StatusCode == System.Net.HttpStatusCode.Created);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure container {Container} exists", containerName);
        }
    }
}
