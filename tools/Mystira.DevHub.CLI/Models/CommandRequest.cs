using System.Text.Json;

namespace Mystira.DevHub.CLI.Models;

public class CommandRequest
{
    public string Command { get; set; } = string.Empty;
    public JsonElement Args { get; set; }
}

public class CosmosExportArgs
{
    public string OutputPath { get; set; } = string.Empty;
}

public class CosmosStatsArgs
{
    // No specific args needed for stats
}

public class MigrationArgs
{
    public string Type { get; set; } = string.Empty; // scenarios, bundles, media-metadata, blobs, master-data, all
    public string? SourceCosmosConnection { get; set; }
    public string? DestCosmosConnection { get; set; }
    public string? SourceStorageConnection { get; set; }
    public string? DestStorageConnection { get; set; }
    public string SourceDatabaseName { get; set; } = "MystiraDb";
    public string DestDatabaseName { get; set; } = "MystiraAppDb";
    public string ContainerName { get; set; } = "mystira-app-media";
    public string? JsonFilesPath { get; set; } // Path to master data JSON files
}

public class InfrastructureArgs
{
    public string Action { get; set; } = string.Empty; // validate, preview, deploy, destroy
    public string WorkflowFile { get; set; } = "infrastructure-deploy-dev.yml";
    public string Repository { get; set; } = "phoenixvc/Mystira.App";
    public bool Confirm { get; set; } = false; // For destroy operation
}
