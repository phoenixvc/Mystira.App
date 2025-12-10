using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using System.Diagnostics;

namespace Mystira.DevHub.Services.Migration;

public class MigrationService : IMigrationService
{
    private readonly ILogger<MigrationService> _logger;
    
    // Concurrency limits for parallel operations
    private const int MaxConcurrentDocumentOperations = 100;
    private const int MaxConcurrentBlobOperations = 10;
    private const int MaxRetries = 3;

    public MigrationService(ILogger<MigrationService> logger)
    {
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateScenariosAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting scenarios migration from {SourceDb} to {DestDb}", sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString);

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, "Scenarios");
            var destContainer = destClient.GetContainer(destDatabaseName, "Scenarios");

            // Ensure destination container exists
            await EnsureContainerExists(destClient, destDatabaseName, "Scenarios", "/id");

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

    public async Task<MigrationResult> MigrateContentBundlesAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting content bundles migration from {SourceDb} to {DestDb}", sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString);

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, "ContentBundles");
            var destContainer = destClient.GetContainer(destDatabaseName, "ContentBundles");

            // Ensure destination container exists
            await EnsureContainerExists(destClient, destDatabaseName, "ContentBundles", "/id");

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

    public async Task<MigrationResult> MigrateMediaAssetsAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting media assets migration from {SourceDb} to {DestDb}", sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString);

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, "MediaAssets");
            var destContainer = destClient.GetContainer(destDatabaseName, "MediaAssets");

            // Ensure destination container exists
            await EnsureContainerExists(destClient, destDatabaseName, "MediaAssets", "/id");

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

            // Check if source container exists
            var sourceExists = await sourceContainerClient.ExistsAsync();
            if (!sourceExists.Value)
            {
                _logger.LogWarning("Source container {Container} does not exist, skipping migration", containerName);
                result.Success = true;
                result.Errors.Add($"Source container '{containerName}' does not exist - nothing to migrate");
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            // Ensure destination container exists (use None for private access)
            await destContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // List all blobs in source container
            var blobs = new List<string>();
            await foreach (var blobItem in sourceContainerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }

            result.TotalItems = blobs.Count;
            _logger.LogInformation("Found {Count} blobs to migrate", blobs.Count);

            if (blobs.Count == 0)
            {
                result.Success = true;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            // Migrate blobs with parallel processing and retry logic
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(MaxConcurrentBlobOperations); // Limit concurrent blob operations (blobs can be large)
            int successCount = 0;
            int failureCount = 0;

            foreach (var blobName in blobs)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
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
                            Interlocked.Increment(ref successCount);
                            return;
                        }

                        // Retry logic for blob migration
                        int retryCount = 0;
                        bool success = false;

                        while (!success && retryCount < MaxRetries)
                        {
                            try
                            {
                                // Download from source and upload to destination (works for private containers)
                                var downloadResponse = await sourceBlobClient.DownloadContentAsync();
                                var blobContent = downloadResponse.Value.Content;
                                var contentType = downloadResponse.Value.Details.ContentType;
                                var metadata = downloadResponse.Value.Details.Metadata;

                                await destBlobClient.UploadAsync(blobContent, overwrite: false);

                                // Set content type and metadata if available
                                var headers = new BlobHttpHeaders();
                                if (!string.IsNullOrEmpty(contentType))
                                {
                                    headers.ContentType = contentType;
                                }
                                await destBlobClient.SetHttpHeadersAsync(headers);

                                if (metadata != null && metadata.Count > 0)
                                {
                                    await destBlobClient.SetMetadataAsync(metadata);
                                }

                                success = true;
                                Interlocked.Increment(ref successCount);
                                _logger.LogDebug("Migrated blob: {BlobName}", blobName);
                            }
                            catch (Azure.RequestFailedException ex) when (ex.Status == 429 || ex.Status == 503)
                            {
                                // Rate limiting or service unavailable - retry with exponential backoff
                                retryCount++;
                                if (retryCount < MaxRetries)
                                {
                                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                                    _logger.LogWarning("Rate limited or service unavailable for blob {BlobName}, retry {Retry}/{MaxRetries} after {Delay}s", 
                                        blobName, retryCount, MaxRetries, delay.TotalSeconds);
                                    await Task.Delay(delay);
                                }
                                else
                                {
                                    throw; // Max retries exceeded
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        lock (result.Errors)
                        {
                            result.Errors.Add($"Failed to migrate blob {blobName}: {ex.Message}");
                        }
                        _logger.LogError(ex, "Failed to migrate blob {BlobName}", blobName);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            // Wait for all blob migrations to complete
            await Task.WhenAll(tasks);

            result.SuccessCount = successCount;
            result.FailureCount = failureCount;
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

    /// <summary>
    /// Generic container migration using dynamic JSON documents.
    /// Works for any container without needing specific model classes.
    /// </summary>
    public async Task<MigrationResult> MigrateContainerAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName, string containerName, string partitionKeyPath = "/id", bool dryRun = false)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting{DryRun} generic migration for container: {Container} from {SourceDb} to {DestDb}", 
                dryRun ? " DRY-RUN" : "", containerName, sourceDatabaseName, destDatabaseName);

            var sourceOptions = new CosmosClientOptions { AllowBulkExecution = false }; // Read doesn't need bulk
            var destOptions = new CosmosClientOptions { AllowBulkExecution = true }; // Enable bulk for writes

            using var sourceClient = new CosmosClient(sourceConnectionString, sourceOptions);
            using var destClient = new CosmosClient(destConnectionString, destOptions);

            // Check if source container exists before attempting migration
            var sourceContainerExists = await CheckContainerExists(sourceClient, sourceDatabaseName, containerName);
            if (!sourceContainerExists)
            {
                _logger.LogWarning("Source container {Container} does not exist in database {Database}, skipping migration", containerName, sourceDatabaseName);
                result.Success = true;
                result.Errors.Add($"Source container '{containerName}' does not exist in database '{sourceDatabaseName}' - nothing to migrate");
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (!dryRun)
            {
                // Ensure destination container exists
                await EnsureContainerExists(destClient, destDatabaseName, containerName, partitionKeyPath);
            }

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, containerName);

            // Query all items from source as dynamic JSON
            var query = sourceContainer.GetItemQueryIterator<dynamic>("SELECT * FROM c");
            var items = new List<dynamic>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                items.AddRange(response);
            }

            result.TotalItems = items.Count;
            _logger.LogInformation("Found {Count} items to migrate in {Container}", items.Count, containerName);

            if (items.Count == 0)
            {
                _logger.LogInformation("Container {Container} is empty, nothing to migrate", containerName);
                result.Success = true;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (dryRun)
            {
                _logger.LogInformation("DRY-RUN: Would migrate {Count} items from {Container}", items.Count, containerName);
                result.Success = true;
                result.SuccessCount = items.Count;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            var destContainer = destClient.GetContainer(destDatabaseName, containerName);

            // Use bulk operations for better performance
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(MaxConcurrentDocumentOperations); // Limit concurrent operations
            int successCount = 0;
            int failureCount = 0;

            foreach (var item in items)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Extract partition key value from the item
                        string partitionKeyValue = GetPartitionKeyValueSafe(item, partitionKeyPath, _logger);

                        // Retry logic with exponential backoff
                        int retryCount = 0;
                        bool success = false;

                        while (!success && retryCount < MaxRetries)
                        {
                            try
                            {
                                await destContainer.UpsertItemAsync(item, new PartitionKey(partitionKeyValue));
                                success = true;
                                Interlocked.Increment(ref successCount);
                            }
                            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            {
                                // Rate limiting - wait and retry with exponential backoff
                                retryCount++;
                                if (retryCount < MaxRetries)
                                {
                                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                                    _logger.LogWarning("Rate limited on item {PartitionKey}, retry {Retry}/{MaxRetries} after {Delay}s", 
                                        partitionKeyValue, retryCount, MaxRetries, delay.TotalSeconds);
                                    await Task.Delay(delay);
                                }
                                else
                                {
                                    throw; // Max retries exceeded
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        string itemId = item?.id?.ToString() ?? "unknown";
                        lock (result.Errors)
                        {
                            result.Errors.Add($"Failed to migrate item {itemId} in {containerName}: {ex.Message}");
                        }
                        _logger.LogError(ex, "Failed to migrate item in {Container}", containerName);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            // Wait for all bulk operations to complete
            await Task.WhenAll(tasks);

            result.SuccessCount = successCount;
            result.FailureCount = failureCount;
            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("{Container} migration completed: {Result}", containerName, result);
            return result;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB error during {Container} migration", containerName);
            result.Success = false;
            result.Errors.Add($"Cosmos DB error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Storage error during {Container} migration", containerName);
            result.Success = false;
            result.Errors.Add($"Azure Storage error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        catch (OperationCanceledException)
        {
            // Propagate task cancellation so higher level handlers can deal with it properly.
            throw;
        }
        catch (Exception ex) when (!(ex is StackOverflowException) && !(ex is OutOfMemoryException) && !(ex is ThreadAbortException))
        {
            _logger.LogError(ex, "Unhandled exception during {Container} migration", containerName);
            result.Success = false;
            result.Errors.Add($"Unhandled exception: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<bool> CheckContainerExists(CosmosClient client, string databaseName, string containerName)
    {
        try
        {
            var container = client.GetContainer(databaseName, containerName);
            await container.ReadContainerAsync();
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private string GetPartitionKeyValue(dynamic item, string partitionKeyPath)
    {
        return GetPartitionKeyValueSafe(item, partitionKeyPath, _logger);
    }

    private static string GetPartitionKeyValueSafe(dynamic item, string partitionKeyPath, ILogger logger)
    {
        // Remove leading slash from partition key path
        var propertyName = partitionKeyPath.TrimStart('/');

        // Handle nested paths (e.g., "/metadata/id")
        var parts = propertyName.Split('/');
        dynamic current = item;

        foreach (var part in parts)
        {
            if (current is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    // Partition key property not found - try fallback to 'id'
                    if (item is System.Text.Json.JsonElement rootElement && rootElement.TryGetProperty("id", out var idProp))
                    {
                        return idProp.ToString();
                    }
                    throw new InvalidOperationException($"Partition key path '{partitionKeyPath}' not found in item and no 'id' fallback available");
                }
            }
            else
            {
                // Try to access as dynamic object
                try
                {
                    // Add a null check before attempting to cast and access
                    if (current == null)
                    {
                        // current is null, fallback to 'id'
                        string? itemId = item?.id?.ToString();
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            return itemId;
                        }
                        throw new InvalidOperationException($"Partition key path '{partitionKeyPath}' is null and no 'id' fallback available");
                    }
                    current = ((IDictionary<string, object>)current)[part];
                }
                catch (InvalidCastException)
                {
                    // Partition key property not found - try fallback to 'id'
                    string? itemId = item?.id?.ToString();
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        return itemId;
                    }
                    throw new InvalidOperationException($"Partition key path '{partitionKeyPath}' not found in item and no 'id' fallback available");
                }
                catch (KeyNotFoundException)
                {
                    string? itemId = item?.id?.ToString();
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        return itemId;
                    }
                    throw new InvalidOperationException($"Partition key path '{partitionKeyPath}' not found in item and no 'id' fallback available");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unexpected exception when traversing partition key path '{PartitionKeyPath}'", partitionKeyPath);
                    string? itemId = item?.id?.ToString();
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        return itemId;
                    }
                    throw new InvalidOperationException($"Unexpected exception traversing partition key path '{partitionKeyPath}' and no 'id' fallback available", ex);
                }
            }
        }

        var result = current?.ToString();
        if (string.IsNullOrEmpty(result))
        {
            throw new InvalidOperationException($"Partition key value at path '{partitionKeyPath}' is null or empty");
        }
        return result;
    }

    private async Task EnsureContainerExists(CosmosClient client, string databaseName, string containerName, string partitionKeyPath)
    {
        try
        {
            // First ensure the database exists
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            _logger.LogInformation("Database {Database} ready (created: {Created})", databaseName, databaseResponse.StatusCode == System.Net.HttpStatusCode.Created);

            // Then ensure the container exists
            var database = client.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerName, partitionKeyPath);
            _logger.LogInformation("Container {Container} ready (created: {Created})", containerName, containerResponse.StatusCode == System.Net.HttpStatusCode.Created);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure container {Container} exists in database {Database}", containerName, databaseName);
        }
    }

    public async Task<MigrationResult> SeedMasterDataAsync(string destConnectionString, string databaseName, string jsonFilesPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting master data seeding to {Database}", databaseName);

            using var destClient = new CosmosClient(destConnectionString);

            // Seed all master data types
            var compassAxisResult = await SeedCompassAxesAsync(destClient, databaseName, jsonFilesPath);
            var archetypeResult = await SeedArchetypesAsync(destClient, databaseName, jsonFilesPath);
            var echoTypeResult = await SeedEchoTypesAsync(destClient, databaseName, jsonFilesPath);
            var fantasyThemeResult = await SeedFantasyThemesAsync(destClient, databaseName, jsonFilesPath);
            var ageGroupResult = await SeedAgeGroupsAsync(destClient, databaseName, jsonFilesPath);

            result.TotalItems = compassAxisResult.TotalItems + archetypeResult.TotalItems +
                               echoTypeResult.TotalItems + fantasyThemeResult.TotalItems + ageGroupResult.TotalItems;
            result.SuccessCount = compassAxisResult.SuccessCount + archetypeResult.SuccessCount +
                                 echoTypeResult.SuccessCount + fantasyThemeResult.SuccessCount + ageGroupResult.SuccessCount;
            result.FailureCount = compassAxisResult.FailureCount + archetypeResult.FailureCount +
                                 echoTypeResult.FailureCount + fantasyThemeResult.FailureCount + ageGroupResult.FailureCount;
            result.Errors.AddRange(compassAxisResult.Errors);
            result.Errors.AddRange(archetypeResult.Errors);
            result.Errors.AddRange(echoTypeResult.Errors);
            result.Errors.AddRange(fantasyThemeResult.Errors);
            result.Errors.AddRange(ageGroupResult.Errors);

            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Master data seeding completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during master data seeding");
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<MigrationResult> SeedCompassAxesAsync(CosmosClient client, string databaseName, string jsonFilesPath)
    {
        var result = new MigrationResult();
        var containerName = "CompassAxes";
        var jsonFile = Path.Combine(jsonFilesPath, "CoreAxes.json");

        try
        {
            await EnsureContainerExists(client, databaseName, containerName, "/id");
            var container = client.GetContainer(databaseName, containerName);

            if (!File.Exists(jsonFile))
            {
                _logger.LogWarning("CoreAxes.json not found at {Path}", jsonFile);
                return result;
            }

            var json = await File.ReadAllTextAsync(jsonFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<JsonValueItem>>(json);

            if (items == null) return result;

            result.TotalItems = items.Count;

            foreach (var item in items)
            {
                try
                {
                    var entity = new CompassAxis
                    {
                        Id = GenerateDeterministicId("compass-axis", item.Value),
                        Name = item.Value,
                        Description = $"Compass axis: {item.Value}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await container.UpsertItemAsync(entity, new PartitionKey(entity.Id));
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to seed compass axis {item.Value}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to seed compass axes: {ex.Message}");
        }

        return result;
    }

    private async Task<MigrationResult> SeedArchetypesAsync(CosmosClient client, string databaseName, string jsonFilesPath)
    {
        var result = new MigrationResult();
        var containerName = "ArchetypeDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "Archetypes.json");

        try
        {
            await EnsureContainerExists(client, databaseName, containerName, "/id");
            var container = client.GetContainer(databaseName, containerName);

            if (!File.Exists(jsonFile))
            {
                _logger.LogWarning("Archetypes.json not found at {Path}", jsonFile);
                return result;
            }

            var json = await File.ReadAllTextAsync(jsonFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<JsonValueItem>>(json);

            if (items == null) return result;

            result.TotalItems = items.Count;

            foreach (var item in items)
            {
                try
                {
                    var entity = new ArchetypeDefinition
                    {
                        Id = GenerateDeterministicId("archetype", item.Value),
                        Name = item.Value,
                        Description = $"Archetype: {item.Value}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await container.UpsertItemAsync(entity, new PartitionKey(entity.Id));
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to seed archetype {item.Value}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to seed archetypes: {ex.Message}");
        }

        return result;
    }

    private async Task<MigrationResult> SeedEchoTypesAsync(CosmosClient client, string databaseName, string jsonFilesPath)
    {
        var result = new MigrationResult();
        var containerName = "EchoTypeDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "EchoTypes.json");

        try
        {
            await EnsureContainerExists(client, databaseName, containerName, "/id");
            var container = client.GetContainer(databaseName, containerName);

            if (!File.Exists(jsonFile))
            {
                _logger.LogWarning("EchoTypes.json not found at {Path}", jsonFile);
                return result;
            }

            var json = await File.ReadAllTextAsync(jsonFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<JsonValueItem>>(json);

            if (items == null) return result;

            result.TotalItems = items.Count;

            foreach (var item in items)
            {
                try
                {
                    var entity = new EchoTypeDefinition
                    {
                        Id = GenerateDeterministicId("echo-type", item.Value),
                        Name = item.Value,
                        Description = $"Echo type: {item.Value}",
                        Category = GetEchoTypeCategory(item.Value),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await container.UpsertItemAsync(entity, new PartitionKey(entity.Id));
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to seed echo type {item.Value}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to seed echo types: {ex.Message}");
        }

        return result;
    }

    private async Task<MigrationResult> SeedFantasyThemesAsync(CosmosClient client, string databaseName, string jsonFilesPath)
    {
        var result = new MigrationResult();
        var containerName = "FantasyThemeDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "FantasyThemes.json");

        try
        {
            await EnsureContainerExists(client, databaseName, containerName, "/id");
            var container = client.GetContainer(databaseName, containerName);

            if (!File.Exists(jsonFile))
            {
                _logger.LogWarning("FantasyThemes.json not found at {Path}", jsonFile);
                return result;
            }

            var json = await File.ReadAllTextAsync(jsonFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<JsonValueItem>>(json);

            if (items == null) return result;

            result.TotalItems = items.Count;

            foreach (var item in items)
            {
                try
                {
                    var entity = new FantasyThemeDefinition
                    {
                        Id = GenerateDeterministicId("fantasy-theme", item.Value),
                        Name = item.Value,
                        Description = $"Fantasy theme: {item.Value}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await container.UpsertItemAsync(entity, new PartitionKey(entity.Id));
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to seed fantasy theme {item.Value}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to seed fantasy themes: {ex.Message}");
        }

        return result;
    }

    private async Task<MigrationResult> SeedAgeGroupsAsync(CosmosClient client, string databaseName, string jsonFilesPath)
    {
        var result = new MigrationResult();
        var containerName = "AgeGroupDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "AgeGroups.json");

        try
        {
            await EnsureContainerExists(client, databaseName, containerName, "/id");
            var container = client.GetContainer(databaseName, containerName);

            if (!File.Exists(jsonFile))
            {
                _logger.LogWarning("AgeGroups.json not found at {Path}", jsonFile);
                return result;
            }

            var json = await File.ReadAllTextAsync(jsonFile);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<AgeGroupJsonItem>>(json);

            if (items == null) return result;

            result.TotalItems = items.Count;

            foreach (var item in items)
            {
                try
                {
                    var entity = new AgeGroupDefinition
                    {
                        Id = GenerateDeterministicId("age-group", item.Value),
                        Name = item.Name,
                        Value = item.Value,
                        MinimumAge = item.MinimumAge,
                        MaximumAge = item.MaximumAge,
                        Description = $"Age group for ages {item.MinimumAge}-{item.MaximumAge}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await container.UpsertItemAsync(entity, new PartitionKey(entity.Id));
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Failed to seed age group {item.Value}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to seed age groups: {ex.Message}");
        }

        return result;
    }

    private static string GenerateDeterministicId(string entityType, string name)
    {
        var input = $"{entityType}:{name.ToLowerInvariant()}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes).ToString();
    }

    /// <summary>
    /// Categorizes echo types into logical groups for better organization.
    /// Categories: moral, emotional, behavioral, social, cognitive, identity, meta
    /// </summary>
    private static string GetEchoTypeCategory(string echoType)
    {
        // Moral echo types - related to ethical choices and values
        var moralTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "honesty", "deception", "loyalty", "betrayal", "justice", "injustice",
            "fairness", "bias", "forgiveness", "revenge", "sacrifice", "selfishness",
            "obedience", "rebellion", "promise", "oath_made", "oath_broken",
            "lie_exposed", "secret_revealed", "first_blood"
        };

        // Emotional echo types - related to feelings and emotional states
        var emotionalTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "doubt", "confidence", "shame", "pride", "regret", "hope", "despair",
            "grief", "denial", "acceptance", "awakening", "resignation", "fear",
            "panic", "jealousy", "envy", "gratitude", "resentment", "love"
        };

        // Behavioral echo types - related to actions and conduct
        var behavioralTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "growth", "stagnation", "kindness", "neglect", "compassion", "coldness",
            "generosity", "bravery", "aggression", "cowardice", "protection",
            "avoidance", "confrontation", "flight", "freeze", "rescue",
            "denial_of_help", "risk_taking", "resilience"
        };

        // Social echo types - related to interactions and relationships
        var socialTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "trust", "manipulation", "support", "abandonment", "listening",
            "interrupting", "mockery", "encouragement", "humiliation", "respect",
            "disrespect", "sharing", "withholding", "blaming", "apologizing"
        };

        // Cognitive echo types - related to thinking and understanding
        var cognitiveTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "curiosity", "closed-mindedness", "truth_seeking", "value_conflict",
            "reflection", "projection", "mirroring", "internalization",
            "breakthrough", "denial_of_truth", "clarity", "lesson_learned",
            "lesson_ignored", "destiny_revealed"
        };

        // Identity echo types - related to self and persona
        var identityTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authenticity", "masking", "conformity", "individualism",
            "dependence", "independence", "attention_seeking", "withdrawal",
            "role_adoption", "role_rejection", "role_locked"
        };

        // Meta/System echo types - game mechanics and meta concepts
        var metaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pattern_repetition", "pattern_break", "echo_amplification",
            "influence_spread", "echo_collision", "legacy_creation",
            "reputation_change", "morality_shift", "alignment_pull", "world_change",
            "rule_checker", "what_if_scientist", "try_again_hero", "tidy_expert",
            "helper_captain_coop", "rhythm_explorer"
        };

        if (moralTypes.Contains(echoType)) return "moral";
        if (emotionalTypes.Contains(echoType)) return "emotional";
        if (behavioralTypes.Contains(echoType)) return "behavioral";
        if (socialTypes.Contains(echoType)) return "social";
        if (cognitiveTypes.Contains(echoType)) return "cognitive";
        if (identityTypes.Contains(echoType)) return "identity";
        if (metaTypes.Contains(echoType)) return "meta";

        return "other";
    }

    private class JsonValueItem
    {
        public string Value { get; set; } = string.Empty;
    }

    private class AgeGroupJsonItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int MinimumAge { get; set; }
        public int MaximumAge { get; set; }
    }
}
