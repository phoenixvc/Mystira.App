using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Royalties.Commands;

/// <summary>
/// Handler for PayRoyaltyCommand
/// </summary>
public class PayRoyaltyCommandHandler : ICommandHandler<PayRoyaltyCommand, RoyaltyPaymentResult>
{
    private readonly IStoryProtocolService _storyProtocolService;
    private readonly ILogger<PayRoyaltyCommandHandler> _logger;

    public PayRoyaltyCommandHandler(
        IStoryProtocolService storyProtocolService,
        ILogger<PayRoyaltyCommandHandler> logger)
    {
        _storyProtocolService = storyProtocolService;
        _logger = logger;
    }

    public async Task<RoyaltyPaymentResult> Handle(PayRoyaltyCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IpAssetId))
        {
            throw new ArgumentException("IP Asset ID cannot be null or empty", nameof(request.IpAssetId));
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));
        }

        _logger.LogInformation(
            "Processing royalty payment of {Amount} to IP Asset: {IpAssetId}",
            request.Amount, request.IpAssetId);

        return await _storyProtocolService.PayRoyaltyAsync(
            request.IpAssetId,
            request.Amount,
            request.PayerReference);
    }
}
