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
                        Category = "other",
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
