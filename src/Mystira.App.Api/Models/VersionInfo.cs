using System.Reflection;

namespace Mystira.App.Api.Models;

/// <summary>
/// Represents the version information of the API
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// The version number of the API
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the API
    /// </summary>
    public string ApiName { get; set; } = string.Empty;
    
    /// <summary>
    /// The build date of the API
    /// </summary>
    public string BuildDate { get; set; } = string.Empty;
    
    /// <summary>
    /// The environment the API is running in
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}
