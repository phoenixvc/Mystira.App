namespace Mystira.App.Api.Models;

/// <summary>
/// Configuration model for application status and version management
/// </summary>
public class AppStatusConfiguration
{
    public string MinSupportedVersion { get; set; } = "1.0.0";
    public string LatestVersion { get; set; } = "1.0.0";
    public string ContentVersion { get; set; } = string.Empty;
    public string BundleVersion { get; set; } = "1.0.0";
    public bool ForceContentRefresh { get; set; } = false;
    public bool MaintenanceMode { get; set; } = false;
    public string? MaintenanceMessage { get; set; } = string.Empty;
    public string? UpdateMessage { get; set; } = string.Empty;
    public FeatureFlags FeatureFlags { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Feature flags for controlling application functionality
/// </summary>
public class FeatureFlags
{
    public bool EnableNewScenarios { get; set; } = true;
    public bool EnableMediaPreview { get; set; } = true;
    public bool EnableBulkUpload { get; set; } = true;
    public bool EnableBundleUpload { get; set; } = true;
}
