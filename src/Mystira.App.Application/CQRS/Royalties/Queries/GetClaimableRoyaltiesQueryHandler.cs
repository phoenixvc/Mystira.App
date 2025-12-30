using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Royalties.Queries;

/// <summary>
/// Wolverine message handler for retrieving claimable royalties for an IP asset.
/// </summary>
public static class GetClaimableRoyaltiesQueryHandler
{
    public static async Task<RoyaltyBalance> Handle(
        GetClaimableRoyaltiesQuery request,
        IStoryProtocolService storyProtocolService,
        ILogger<GetClaimableRoyaltiesQuery> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IpAssetId))
        {
            throw new ArgumentException("IP Asset ID cannot be null or empty", nameof(request.IpAssetId));
        }

        logger.LogInformation("Getting claimable royalties for IP Asset: {IpAssetId}", request.IpAssetId);

        return await storyProtocolService.GetClaimableRoyaltiesAsync(request.IpAssetId);
    }
}
