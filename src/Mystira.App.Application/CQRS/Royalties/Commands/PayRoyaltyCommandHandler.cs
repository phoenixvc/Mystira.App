using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Royalties.Commands;

/// <summary>
/// Wolverine message handler for paying royalties to IP assets.
/// </summary>
public static class PayRoyaltyCommandHandler
{
    public static async Task<RoyaltyPaymentResult> Handle(
        PayRoyaltyCommand request,
        IStoryProtocolService storyProtocolService,
        ILogger<PayRoyaltyCommand> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IpAssetId))
        {
            throw new ArgumentException("IP Asset ID cannot be null or empty", nameof(request.IpAssetId));
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));
        }

        logger.LogInformation(
            "Processing royalty payment of {Amount} to IP Asset: {IpAssetId}",
            request.Amount, request.IpAssetId);

        return await storyProtocolService.PayRoyaltyAsync(
            request.IpAssetId,
            request.Amount,
            request.PayerReference);
    }
}
