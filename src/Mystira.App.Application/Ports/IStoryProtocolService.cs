using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports;

/// <summary>
/// Interface for Story Protocol blockchain integration service
/// </summary>
public interface IStoryProtocolService
{
    /// <summary>
    /// Registers a content piece (scenario or bundle) as an IP Asset on Story Protocol
    /// </summary>
    /// <param name="contentId">ID of the scenario or bundle</param>
    /// <param name="contentTitle">Title of the content</param>
    /// <param name="contributors">List of contributors with royalty splits</param>
    /// <param name="metadataUri">Optional URI to additional metadata</param>
    /// <param name="licenseTermsId">Optional license terms ID</param>
    /// <returns>Story Protocol metadata including IP Asset ID</returns>
    Task<StoryProtocolMetadata> RegisterIpAssetAsync(
        string contentId,
        string contentTitle,
        List<Contributor> contributors,
        string? metadataUri = null,
        string? licenseTermsId = null);

    /// <summary>
    /// Checks if content is already registered on Story Protocol
    /// </summary>
    /// <param name="contentId">ID of the scenario or bundle</param>
    /// <returns>True if registered, false otherwise</returns>
    Task<bool> IsRegisteredAsync(string contentId);

    /// <summary>
    /// Gets the current royalty split configuration from Story Protocol
    /// </summary>
    /// <param name="ipAssetId">Story Protocol IP Asset ID</param>
    /// <returns>Current royalty configuration</returns>
    Task<StoryProtocolMetadata?> GetRoyaltyConfigurationAsync(string ipAssetId);

    /// <summary>
    /// Updates the royalty split for an existing IP Asset
    /// Note: This may not be allowed depending on the license terms
    /// </summary>
    /// <param name="ipAssetId">Story Protocol IP Asset ID</param>
    /// <param name="contributors">Updated list of contributors</param>
    /// <returns>Updated Story Protocol metadata</returns>
    Task<StoryProtocolMetadata> UpdateRoyaltySplitAsync(string ipAssetId, List<Contributor> contributors);
}
