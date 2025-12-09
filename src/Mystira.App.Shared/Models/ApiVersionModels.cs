namespace Mystira.App.Shared.Models;

/// <summary>
/// API version information returned by the /api/apiinfo endpoint.
/// </summary>
public class ApiVersionInfo
{
    public string ApiVersion { get; set; } = string.Empty;
    public string BuildVersion { get; set; } = string.Empty;
    public string MasterDataVersion { get; set; } = string.Empty;
    public bool SupportsLegacyEnums { get; set; }
    public MasterDataEntityInfo[] MasterDataEntities { get; set; } = Array.Empty<MasterDataEntityInfo>();
    public DeprecatedApiInfo[] DeprecatedApis { get; set; } = Array.Empty<DeprecatedApiInfo>();
}

/// <summary>
/// Information about a master data entity.
/// </summary>
public class MasterDataEntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int? Count { get; set; }
}

/// <summary>
/// Information about a deprecated API endpoint.
/// </summary>
public class DeprecatedApiInfo
{
    public string Endpoint { get; set; } = string.Empty;
    public string ReplacedBy { get; set; } = string.Empty;
    public string DeprecationDate { get; set; } = string.Empty;
    public string RemovalDate { get; set; } = string.Empty;
}

/// <summary>
/// Legacy mappings for backward compatibility with old enum-based values.
/// </summary>
public class LegacyMappings
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> CoreAxisMappings { get; set; } = new();
    public Dictionary<string, string> ArchetypeMappings { get; set; } = new();
}
