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

    public MigrationService(ILogger<MigrationService> logger)
    {
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateScenariosAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName, MigrationOptions? options = null)
    {
        options ??= new MigrationOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();
        var mode = options.DryRun ? "[DRY RUN] " : "";

        try
        {
            _logger.LogInformation("{Mode}Starting scenarios migration from {SourceDb} to {DestDb}", mode, sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString, new CosmosClientOptions { AllowBulkExecution = options.UseBulkOperations && !options.DryRun });

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, "Scenarios");

            if (!options.DryRun)
            {
                await EnsureContainerExists(destClient, destDatabaseName, "Scenarios", "/id");
            }
            var destContainer = destClient.GetContainer(destDatabaseName, "Scenarios");

            var query = sourceContainer.GetItemQueryIterator<Scenario>("SELECT * FROM c");
            var scenarios = new List<Scenario>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                scenarios.AddRange(response);
            }

            result.TotalItems = scenarios.Count;
            _logger.LogInformation("{Mode}Found {Count} scenarios to migrate", mode, scenarios.Count);

            if (options.DryRun)
            {
                result.Success = true;
                result.SuccessCount = scenarios.Count;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (options.UseBulkOperations && scenarios.Count > 10)
            {
                await MigrateBulkTypedAsync(destContainer, scenarios, s => s.Id, result, options);
            }
            else
            {
                await MigrateSequentialTypedAsync(destContainer, scenarios, s => s.Id, result, options);
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

    public async Task<MigrationResult> MigrateContentBundlesAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName, MigrationOptions? options = null)
    {
        options ??= new MigrationOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();
        var mode = options.DryRun ? "[DRY RUN] " : "";

        try
        {
            _logger.LogInformation("{Mode}Starting content bundles migration from {SourceDb} to {DestDb}", mode, sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString, new CosmosClientOptions { AllowBulkExecution = options.UseBulkOperations && !options.DryRun });

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, "ContentBundles");

            if (!options.DryRun)
            {
                await EnsureContainerExists(destClient, destDatabaseName, "ContentBundles", "/id");
            }
            var destContainer = destClient.GetContainer(destDatabaseName, "ContentBundles");

            var query = sourceContainer.GetItemQueryIterator<ContentBundle>("SELECT * FROM c");
            var bundles = new List<ContentBundle>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                bundles.AddRange(response);
            }

            result.TotalItems = bundles.Count;
            _logger.LogInformation("{Mode}Found {Count} content bundles to migrate", mode, bundles.Count);

            if (options.DryRun)
            {
                result.Success = true;
                result.SuccessCount = bundles.Count;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (options.UseBulkOperations && bundles.Count > 10)
            {
                await MigrateBulkTypedAsync(destContainer, bundles, b => b.Id, result, options);
            }
            else
            {
                await MigrateSequentialTypedAsync(destContainer, bundles, b => b.Id, result, options);
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

    public async Task<MigrationResult> MigrateMediaAssetsAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName, MigrationOptions? options = null)
    {
        options ??= new MigrationOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();
        var mode = options.DryRun ? "[DRY RUN] " : "";

        try
        {
            _logger.LogInformation("{Mode}Starting media assets migration from {SourceDb} to {DestDb}", mode, sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString, new CosmosClientOptions { AllowBulkExecution = options.UseBulkOperations && !options.DryRun });

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, "MediaAssets");

            if (!options.DryRun)
            {
                await EnsureContainerExists(destClient, destDatabaseName, "MediaAssets", "/id");
            }
            var destContainer = destClient.GetContainer(destDatabaseName, "MediaAssets");

            var query = sourceContainer.GetItemQueryIterator<MediaAsset>("SELECT * FROM c");
            var assets = new List<MediaAsset>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                assets.AddRange(response);
            }

            result.TotalItems = assets.Count;
            _logger.LogInformation("{Mode}Found {Count} media assets to migrate", mode, assets.Count);

            if (options.DryRun)
            {
                result.Success = true;
                result.SuccessCount = assets.Count;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (options.UseBulkOperations && assets.Count > 10)
            {
                await MigrateBulkTypedAsync(destContainer, assets, a => a.Id, result, options);
            }
            else
            {
                await MigrateSequentialTypedAsync(destContainer, assets, a => a.Id, result, options);
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

    public async Task<MigrationResult> MigrateBlobStorageAsync(string sourceStorageConnectionString, string destStorageConnectionString, string containerName, MigrationOptions? options = null)
    {
        options ??= new MigrationOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();
        var mode = options.DryRun ? "[DRY RUN] " : "";

        try
        {
            _logger.LogInformation("{Mode}Starting blob storage migration for container: {Container}", mode, containerName);

            var sourceBlobServiceClient = new BlobServiceClient(sourceStorageConnectionString);
            var destBlobServiceClient = new BlobServiceClient(destStorageConnectionString);

            var sourceContainerClient = sourceBlobServiceClient.GetBlobContainerClient(containerName);
            var destContainerClient = destBlobServiceClient.GetBlobContainerClient(containerName);

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

            if (!options.DryRun)
            {
                await destContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            }

            var blobs = new List<string>();
            await foreach (var blobItem in sourceContainerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }

            result.TotalItems = blobs.Count;
            _logger.LogInformation("{Mode}Found {Count} blobs to migrate", mode, blobs.Count);

            if (options.DryRun)
            {
                result.Success = true;
                result.SuccessCount = blobs.Count;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (blobs.Count == 0)
            {
                result.Success = true;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            // Migrate blobs with parallel processing and retry logic
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(MaxConcurrentBlobOperations);
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

                        while (!success && retryCount < options.MaxRetries)
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
                                if (retryCount < options.MaxRetries)
                                {
                                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                                    _logger.LogWarning("Rate limited or service unavailable for blob {BlobName}, retry {Retry}/{MaxRetries} after {Delay}s",
                                        blobName, retryCount, options.MaxRetries, delay.TotalSeconds);
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

    public async Task<MigrationResult> MigrateContainerAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName, string containerName, string partitionKeyPath = "/id", MigrationOptions? options = null)
    {
        options ??= new MigrationOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();
        var mode = options.DryRun ? "[DRY RUN] " : "";

        try
        {
            _logger.LogInformation("{Mode}Starting generic migration for container: {Container} from {SourceDb} to {DestDb}", mode, containerName, sourceDatabaseName, destDatabaseName);

            using var sourceClient = new CosmosClient(sourceConnectionString);
            using var destClient = new CosmosClient(destConnectionString, new CosmosClientOptions { AllowBulkExecution = options.UseBulkOperations && !options.DryRun });

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

            if (!options.DryRun)
            {
                await EnsureContainerExists(destClient, destDatabaseName, containerName, partitionKeyPath);
            }

            var sourceContainer = sourceClient.GetContainer(sourceDatabaseName, containerName);

            var query = sourceContainer.GetItemQueryIterator<dynamic>("SELECT * FROM c");
            var items = new List<dynamic>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                items.AddRange(response);
            }

            result.TotalItems = items.Count;
            _logger.LogInformation("{Mode}Found {Count} items to migrate in {Container}", mode, items.Count, containerName);

            if (items.Count == 0)
            {
                _logger.LogInformation("Container {Container} is empty, nothing to migrate", containerName);
                result.Success = true;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            if (options.DryRun)
            {
                result.Success = true;
                result.SuccessCount = items.Count;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                _logger.LogInformation("[DRY RUN] Would migrate {Count} items in {Container}", items.Count, containerName);
                return result;
            }

            var destContainer = destClient.GetContainer(destDatabaseName, containerName);

            if (options.UseBulkOperations && items.Count > 10)
            {
                await MigrateBulkDynamicAsync(destContainer, items, partitionKeyPath, result, options, containerName);
            }
            else
            {
                await MigrateSequentialDynamicAsync(destContainer, items, partitionKeyPath, result, options, containerName);
            }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during {Container} migration", containerName);
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    // Bulk migration for typed entities
    private async Task MigrateBulkTypedAsync<T>(Container destContainer, List<T> items, Func<T, string> getPartitionKey, MigrationResult result, MigrationOptions options) where T : class
    {
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var item in items)
        {
            var pk = getPartitionKey(item);
            tasks.Add(destContainer.UpsertItemAsync(item, new PartitionKey(pk))
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref failureCount);
                        lock (result.Errors)
                        {
                            result.Errors.Add($"Failed to migrate item: {t.Exception?.InnerException?.Message}");
                        }
                    }
                }));

            if (tasks.Count >= options.BulkBatchSize)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        result.SuccessCount = successCount;
        result.FailureCount = failureCount;
    }

    // Sequential migration for typed entities with retry
    private async Task MigrateSequentialTypedAsync<T>(Container destContainer, List<T> items, Func<T, string> getPartitionKey, MigrationResult result, MigrationOptions options) where T : class
    {
        foreach (var item in items)
        {
            bool success = false;
            Exception? lastException = null;

            for (int retry = 0; retry <= options.MaxRetries && !success; retry++)
            {
                try
                {
                    var pk = getPartitionKey(item);
                    await destContainer.UpsertItemAsync(item, new PartitionKey(pk));
                    result.SuccessCount++;
                    success = true;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retry < options.MaxRetries)
                {
                    var retryAfter = ex.RetryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, retry));
                    await Task.Delay(retryAfter);
                    lastException = ex;
                }
                catch (Exception ex) when (retry < options.MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                    await Task.Delay(delay);
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (!success)
            {
                result.FailureCount++;
                result.Errors.Add($"Failed to migrate item after {options.MaxRetries} retries: {lastException?.Message}");
            }
        }
    }

    // Bulk migration for dynamic items
    private async Task MigrateBulkDynamicAsync(Container destContainer, List<dynamic> items, string partitionKeyPath, MigrationResult result, MigrationOptions options, string containerName)
    {
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var item in items)
        {
            try
            {
                string partitionKeyValue = GetPartitionKeyValue(item, partitionKeyPath);
                tasks.Add(destContainer.UpsertItemAsync(item, new PartitionKey(partitionKeyValue))
                    .ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref failureCount);
                            string itemId = item?.id?.ToString() ?? "unknown";
                            lock (result.Errors)
                            {
                                result.Errors.Add($"Failed to migrate item {itemId}: {t.Exception?.InnerException?.Message}");
                            }
                            _logger.LogError(t.Exception?.InnerException, "Failed to bulk migrate item in {Container}", containerName);
                        }
                    }));

                if (tasks.Count >= options.BulkBatchSize)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                string itemId = item?.id?.ToString() ?? "unknown";
                lock (result.Errors)
                {
                    result.Errors.Add($"Failed to prepare item {itemId} for migration: {ex.Message}");
                }
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        result.SuccessCount = successCount;
        result.FailureCount = failureCount;
    }

    // Sequential migration for dynamic items with retry
    private async Task MigrateSequentialDynamicAsync(Container destContainer, List<dynamic> items, string partitionKeyPath, MigrationResult result, MigrationOptions options, string containerName)
    {
        foreach (var item in items)
        {
            bool success = false;
            Exception? lastException = null;

            for (int retry = 0; retry <= options.MaxRetries && !success; retry++)
            {
                try
                {
                    string partitionKeyValue = GetPartitionKeyValue(item, partitionKeyPath);
                    await destContainer.UpsertItemAsync(item, new PartitionKey(partitionKeyValue));
                    result.SuccessCount++;
                    success = true;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retry < options.MaxRetries)
                {
                    var retryAfter = ex.RetryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, retry));
                    _logger.LogWarning("Rate limited, retrying after {Delay}ms (attempt {Retry}/{MaxRetries})", retryAfter.TotalMilliseconds, retry + 1, options.MaxRetries);
                    await Task.Delay(retryAfter);
                    lastException = ex;
                }
                catch (Exception ex) when (retry < options.MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                    _logger.LogWarning(ex, "Error migrating item, retrying after {Delay}ms (attempt {Retry}/{MaxRetries})", delay.TotalMilliseconds, retry + 1, options.MaxRetries);
                    await Task.Delay(delay);
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (!success)
            {
                result.FailureCount++;
                string itemId = item?.id?.ToString() ?? "unknown";
                result.Errors.Add($"Failed to migrate item {itemId} in {containerName} after {options.MaxRetries} retries: {lastException?.Message}");
                _logger.LogError(lastException, "Failed to migrate item in {Container} after {MaxRetries} retries", containerName, options.MaxRetries);
            }
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
        var propertyName = partitionKeyPath.TrimStart('/');
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
                    if (item is System.Text.Json.JsonElement rootElement && rootElement.TryGetProperty("id", out var idProp))
                    {
                        return idProp.ToString();
                    }
                    throw new InvalidOperationException($"Partition key path '{partitionKeyPath}' not found in item and no 'id' fallback available");
                }
            }
            else
            {
                try
                {
                    if (current == null)
                    {
                        string? itemId = item?.id?.ToString();
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            return itemId;
                        }
                        throw new InvalidOperationException($"Partition key path '{partitionKeyPath}' is null and no 'id' fallback available");
                    }
                    current = ((IDictionary<string, object>)current)[part];
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
                catch (Exception ex) when (ex is not InvalidOperationException)
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
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            _logger.LogInformation("Database {Database} ready (created: {Created})", databaseName, databaseResponse.StatusCode == System.Net.HttpStatusCode.Created);

            var database = client.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerName, partitionKeyPath);
            _logger.LogInformation("Container {Container} ready (created: {Created})", containerName, containerResponse.StatusCode == System.Net.HttpStatusCode.Created);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure container {Container} exists in database {Database}", containerName, databaseName);
        }
    }

    public async Task<MigrationResult> SeedMasterDataAsync(string destConnectionString, string databaseName, string jsonFilesPath, MigrationOptions? options = null)
    {
        options ??= new MigrationOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult();
        var mode = options.DryRun ? "[DRY RUN] " : "";

        try
        {
            _logger.LogInformation("{Mode}Starting master data seeding to {Database}", mode, databaseName);

            using var destClient = new CosmosClient(destConnectionString);

            var compassAxisResult = await SeedCompassAxesAsync(destClient, databaseName, jsonFilesPath, options);
            var archetypeResult = await SeedArchetypesAsync(destClient, databaseName, jsonFilesPath, options);
            var echoTypeResult = await SeedEchoTypesAsync(destClient, databaseName, jsonFilesPath, options);
            var fantasyThemeResult = await SeedFantasyThemesAsync(destClient, databaseName, jsonFilesPath, options);
            var ageGroupResult = await SeedAgeGroupsAsync(destClient, databaseName, jsonFilesPath, options);

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

    private async Task<MigrationResult> SeedCompassAxesAsync(CosmosClient client, string databaseName, string jsonFilesPath, MigrationOptions options)
    {
        var result = new MigrationResult();
        var containerName = "CompassAxes";
        var jsonFile = Path.Combine(jsonFilesPath, "CoreAxes.json");

        try
        {
            if (!options.DryRun)
            {
                await EnsureContainerExists(client, databaseName, containerName, "/id");
            }
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

            if (options.DryRun)
            {
                result.SuccessCount = items.Count;
                return result;
            }

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

    private async Task<MigrationResult> SeedArchetypesAsync(CosmosClient client, string databaseName, string jsonFilesPath, MigrationOptions options)
    {
        var result = new MigrationResult();
        var containerName = "ArchetypeDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "Archetypes.json");

        try
        {
            if (!options.DryRun)
            {
                await EnsureContainerExists(client, databaseName, containerName, "/id");
            }
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

            if (options.DryRun)
            {
                result.SuccessCount = items.Count;
                return result;
            }

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

    private async Task<MigrationResult> SeedEchoTypesAsync(CosmosClient client, string databaseName, string jsonFilesPath, MigrationOptions options)
    {
        var result = new MigrationResult();
        var containerName = "EchoTypeDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "EchoTypes.json");

        try
        {
            if (!options.DryRun)
            {
                await EnsureContainerExists(client, databaseName, containerName, "/id");
            }
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

            if (options.DryRun)
            {
                result.SuccessCount = items.Count;
                return result;
            }

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

    private async Task<MigrationResult> SeedFantasyThemesAsync(CosmosClient client, string databaseName, string jsonFilesPath, MigrationOptions options)
    {
        var result = new MigrationResult();
        var containerName = "FantasyThemeDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "FantasyThemes.json");

        try
        {
            if (!options.DryRun)
            {
                await EnsureContainerExists(client, databaseName, containerName, "/id");
            }
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

            if (options.DryRun)
            {
                result.SuccessCount = items.Count;
                return result;
            }

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

    private async Task<MigrationResult> SeedAgeGroupsAsync(CosmosClient client, string databaseName, string jsonFilesPath, MigrationOptions options)
    {
        var result = new MigrationResult();
        var containerName = "AgeGroupDefinitions";
        var jsonFile = Path.Combine(jsonFilesPath, "AgeGroups.json");

        try
        {
            if (!options.DryRun)
            {
                await EnsureContainerExists(client, databaseName, containerName, "/id");
            }
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

            if (options.DryRun)
            {
                result.SuccessCount = items.Count;
                return result;
            }

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

    private static string GetEchoTypeCategory(string echoType)
    {
        var moralTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "honesty", "deception", "loyalty", "betrayal", "justice", "injustice",
            "fairness", "bias", "forgiveness", "revenge", "sacrifice", "selfishness",
            "obedience", "rebellion", "promise", "oath_made", "oath_broken",
            "lie_exposed", "secret_revealed", "first_blood"
        };

        var emotionalTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "doubt", "confidence", "shame", "pride", "regret", "hope", "despair",
            "grief", "denial", "acceptance", "awakening", "resignation", "fear",
            "panic", "jealousy", "envy", "gratitude", "resentment", "love"
        };

        var behavioralTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "growth", "stagnation", "kindness", "neglect", "compassion", "coldness",
            "generosity", "bravery", "aggression", "cowardice", "protection",
            "avoidance", "confrontation", "flight", "freeze", "rescue",
            "denial_of_help", "risk_taking", "resilience"
        };

        var socialTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "trust", "manipulation", "support", "abandonment", "listening",
            "interrupting", "mockery", "encouragement", "humiliation", "respect",
            "disrespect", "sharing", "withholding", "blaming", "apologizing"
        };

        var cognitiveTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "curiosity", "closed-mindedness", "truth_seeking", "value_conflict",
            "reflection", "projection", "mirroring", "internalization",
            "breakthrough", "denial_of_truth", "clarity", "lesson_learned",
            "lesson_ignored", "destiny_revealed"
        };

        var identityTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authenticity", "masking", "conformity", "individualism",
            "dependence", "independence", "attention_seeking", "withdrawal",
            "role_adoption", "role_rejection", "role_locked"
        };

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
