namespace Mystira.DevHub.Services.Migration;

public interface IMigrationService
{
    /// <summary>
    /// Migrates scenarios from source to destination Cosmos DB
    /// </summary>
    Task<MigrationResult> MigrateScenariosAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName);

    /// <summary>
    /// Migrates content bundles from source to destination Cosmos DB
    /// </summary>
    Task<MigrationResult> MigrateContentBundlesAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName);

    /// <summary>
    /// Migrates media assets metadata from source to destination Cosmos DB
    /// </summary>
    Task<MigrationResult> MigrateMediaAssetsAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName);

    /// <summary>
    /// Generic container migration - migrates any container using dynamic JSON documents
    /// </summary>
    Task<MigrationResult> MigrateContainerAsync(string sourceConnectionString, string destConnectionString, string sourceDatabaseName, string destDatabaseName, string containerName, string partitionKeyPath = "/id", bool dryRun = false);

    /// <summary>
    /// Copies blob files from source storage to destination storage
    /// </summary>
    Task<MigrationResult> MigrateBlobStorageAsync(string sourceStorageConnectionString, string destStorageConnectionString, string containerName);

    /// <summary>
    /// Seeds master data (CompassAxes, Archetypes, EchoTypes, FantasyThemes, AgeGroups)
    /// from JSON files into production Cosmos DB
    /// </summary>
    Task<MigrationResult> SeedMasterDataAsync(string destConnectionString, string databaseName, string jsonFilesPath);
}

public class MigrationResult
{
    public bool Success { get; set; }
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public override string ToString()
    {
        return $"Migration Result: {SuccessCount}/{TotalItems} successful, {FailureCount} failed, Duration: {Duration.TotalSeconds:F2}s";
    }
}
