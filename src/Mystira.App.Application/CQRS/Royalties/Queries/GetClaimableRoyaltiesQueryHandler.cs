using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Royalties.Queries;

/// <summary>
/// Handler for GetClaimableRoyaltiesQuery
/// </summary>
public class GetClaimableRoyaltiesQueryHandler : IQueryHandler<GetClaimableRoyaltiesQuery, RoyaltyBalance>
{
    private readonly IStoryProtocolService _storyProtocolService;
    private readonly ILogger<GetClaimableRoyaltiesQueryHandler> _logger;

    public GetClaimableRoyaltiesQueryHandler(
        IStoryProtocolService storyProtocolService,
        ILogger<GetClaimableRoyaltiesQueryHandler> logger)
    {
        _storyProtocolService = storyProtocolService;
        _logger = logger;
    }

    public async Task<RoyaltyBalance> Handle(GetClaimableRoyaltiesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IpAssetId))
        {
            throw new ArgumentException("IP Asset ID cannot be null or empty", nameof(request.IpAssetId));
        }

        _logger.LogInformation("Getting claimable royalties for IP Asset: {IpAssetId}", request.IpAssetId);

        return await _storyProtocolService.GetClaimableRoyaltiesAsync(request.IpAssetId);
    }
}
