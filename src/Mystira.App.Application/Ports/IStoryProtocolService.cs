using Mystira.Domain.Models;

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

    /// <summary>
    /// Pays royalties to an IP Asset, distributing to all contributors based on their splits
    /// </summary>
    /// <param name="ipAssetId">Story Protocol IP Asset ID to pay royalties to</param>
    /// <param name="amount">Amount to pay (in WIP token smallest unit)</param>
    /// <param name="payerReference">Optional reference for the payment (e.g., order ID)</param>
    /// <returns>Transaction hash of the royalty payment</returns>
    Task<RoyaltyPaymentResult> PayRoyaltyAsync(string ipAssetId, decimal amount, string? payerReference = null);

    /// <summary>
    /// Gets the claimable royalty balance for an IP Asset
    /// </summary>
    /// <param name="ipAssetId">Story Protocol IP Asset ID</param>
    /// <returns>Claimable balance information</returns>
    Task<RoyaltyBalance> GetClaimableRoyaltiesAsync(string ipAssetId);

    /// <summary>
    /// Claims accumulated royalties for a contributor wallet
    /// </summary>
    /// <param name="ipAssetId">Story Protocol IP Asset ID</param>
    /// <param name="contributorWallet">Wallet address of the contributor claiming</param>
    /// <returns>Transaction hash of the claim</returns>
    Task<string> ClaimRoyaltiesAsync(string ipAssetId, string contributorWallet);
}
