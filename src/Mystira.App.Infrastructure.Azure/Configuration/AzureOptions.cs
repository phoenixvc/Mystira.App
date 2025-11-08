namespace Mystira.App.Infrastructure.Azure.Configuration;

public class AzureOptions
{
    public const string SectionName = "Azure";
    public CosmosDbOptions CosmosDb { get; set; } = new();
    public BlobStorageOptions BlobStorage { get; set; } = new();
}

public class CosmosDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "MystiraAppDb";
    public bool UseInMemoryDatabase { get; set; } = false;
}

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "mystira-app-media";
    public int MaxFileSizeMb { get; set; } = 10;
    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "audio/mpeg", "audio/wav", "audio/ogg",
        "video/mp4", "video/webm", "video/ogg"
    };
}