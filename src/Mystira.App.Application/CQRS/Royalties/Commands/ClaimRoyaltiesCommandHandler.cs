using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Ports;

namespace Mystira.App.Application.CQRS.Royalties.Commands;

/// <summary>
/// Handler for ClaimRoyaltiesCommand
/// </summary>
public class ClaimRoyaltiesCommandHandler : ICommandHandler<ClaimRoyaltiesCommand, string>
{
    private readonly IStoryProtocolService _storyProtocolService;
    private readonly ILogger<ClaimRoyaltiesCommandHandler> _logger;

    public ClaimRoyaltiesCommandHandler(
        IStoryProtocolService storyProtocolService,
        ILogger<ClaimRoyaltiesCommandHandler> logger)
    {
        _storyProtocolService = storyProtocolService;
        _logger = logger;
    }

    public async Task<string> Handle(ClaimRoyaltiesCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IpAssetId))
        {
            throw new ArgumentException("IP Asset ID cannot be null or empty", nameof(request.IpAssetId));
        }

        if (string.IsNullOrWhiteSpace(request.ContributorWallet))
        {
            throw new ArgumentException("Contributor wallet address cannot be null or empty", nameof(request.ContributorWallet));
        }

        _logger.LogInformation(
            "Claiming royalties for wallet {Wallet} from IP Asset: {IpAssetId}",
            request.ContributorWallet, request.IpAssetId);

        return await _storyProtocolService.ClaimRoyaltiesAsync(
            request.IpAssetId,
            request.ContributorWallet);
    }
}
