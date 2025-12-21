using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Configuration.StoryProtocol;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Attribution;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Handler for GetBundleIpStatusQuery - retrieves IP registration status for a content bundle
/// </summary>
public class GetBundleIpStatusQueryHandler : IQueryHandler<GetBundleIpStatusQuery, IpVerificationResponse?>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetBundleIpStatusQueryHandler> _logger;
    private readonly StoryProtocolOptions _options;

    public GetBundleIpStatusQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetBundleIpStatusQueryHandler> logger,
        IOptions<StoryProtocolOptions> options)
    {
        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IpVerificationResponse?> Handle(GetBundleIpStatusQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(request.BundleId));
        }

        var bundle = await _repository.GetByIdAsync(request.BundleId);

        if (bundle == null)
        {
            _logger.LogWarning("Content bundle not found for IP status check: {BundleId}", request.BundleId);
            return null;
        }

        return MapToIpStatusResponse(bundle);
    }

    private IpVerificationResponse MapToIpStatusResponse(ContentBundle bundle)
    {
        var storyProtocol = bundle.StoryProtocol;
        var isRegistered = storyProtocol?.IsRegistered ?? false;

        return new IpVerificationResponse
        {
            ContentId = bundle.Id,
            ContentTitle = bundle.Title,
            IsRegistered = isRegistered,
            IpAssetId = storyProtocol?.IpAssetId,
            RegisteredAt = storyProtocol?.RegisteredAt,
            RegistrationTxHash = storyProtocol?.RegistrationTxHash,
            RoyaltyModuleId = storyProtocol?.RoyaltyModuleId,
            ContributorCount = storyProtocol?.Contributors?.Count ?? 0,
            ExplorerUrl = isRegistered && !string.IsNullOrEmpty(storyProtocol?.IpAssetId)
                ? $"{_options.ExplorerBaseUrl}/address/{storyProtocol.IpAssetId}"
                : null
        };
    }
}
