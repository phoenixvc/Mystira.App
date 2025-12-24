using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Contributors;

/// <summary>
/// Request to register a scenario or bundle as an IP asset on Story Protocol
/// </summary>
public class RegisterIpAssetRequest
{
    /// <summary>
    /// Optional metadata URI pointing to additional IP information (IPFS, etc.)
    /// </summary>
    [StringLength(500)]
    public string? MetadataUri { get; set; }

    /// <summary>
    /// Optional license terms ID if using a specific license template
    /// </summary>
    [StringLength(100)]
    public string? LicenseTermsId { get; set; }
}
