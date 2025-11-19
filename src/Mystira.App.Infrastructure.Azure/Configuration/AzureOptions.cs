namespace Mystira.App.Infrastructure.Azure.Configuration;

public class AzureOptions
{
    public const string SectionName = "Azure";

    /// <summary>
    /// Cosmos DB connection and behaviour options.
    /// These are typically bound from configuration section "Azure:CosmosDb".
    /// </summary>
    public CosmosDbOptions CosmosDb { get; set; } = new();

    /// <summary>
    /// Azure Blob Storage options for media and other assets.
    /// Typically bound from configuration section "Azure:BlobStorage".
    /// </summary>
    public BlobStorageOptions BlobStorage { get; set; } = new();
}

public class CosmosDbOptions
{
    /// <summary>
    /// Cosmos DB connection string.
    /// If omitted in development, the application may fall back to an in-memory database,
    /// depending on startup configuration.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Logical database name used within Cosmos DB.
    /// </summary>
    public string DatabaseName { get; set; } = "MystiraAppDb";

    /// <summary>
    /// When true, the application will use an in-memory database provider instead of Cosmos DB.
    /// This is mainly intended for local development and automated tests.
    /// </summary>
    public bool UseInMemoryDatabase { get; set; } = false;
}

public class BlobStorageOptions
{
    /// <summary>
    /// Connection string for the Azure Storage account where media is stored.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Default container name used for Mystira media assets.
    /// </summary>
    public string ContainerName { get; set; } = "mystira-app-media";

    /// <summary>
    /// Maximum allowed file size (in megabytes) for uploads.
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 10;

    /// <summary>
    /// Allowed MIME types for uploaded files.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "audio/mpeg", "audio/wav", "audio/ogg",
        "video/mp4", "video/webm", "video/ogg"
    };
}
