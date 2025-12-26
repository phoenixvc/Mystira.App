using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;

namespace Mystira.App.Application.CQRS.Royalties.Commands;

/// <summary>
/// Wolverine message handler for claiming royalties from IP assets.
/// </summary>
public static class ClaimRoyaltiesCommandHandler
{
    public static async Task<string> Handle(
        ClaimRoyaltiesCommand request,
        IStoryProtocolService storyProtocolService,
        ILogger<ClaimRoyaltiesCommand> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IpAssetId))
        {
            throw new ArgumentException("IP Asset ID cannot be null or empty", nameof(request.IpAssetId));
        }

        if (string.IsNullOrWhiteSpace(request.ContributorWallet))
        {
            throw new ArgumentException("Contributor wallet address cannot be null or empty", nameof(request.ContributorWallet));
        }

        logger.LogInformation(
            "Claiming royalties for wallet {Wallet} from IP Asset: {IpAssetId}",
            request.ContributorWallet, request.IpAssetId);

        return await storyProtocolService.ClaimRoyaltiesAsync(
            request.IpAssetId,
            request.ContributorWallet);
    }
}
