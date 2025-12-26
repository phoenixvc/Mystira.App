using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Configuration.StoryProtocol;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Attribution;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Wolverine handler for GetBundleIpStatusQuery - retrieves IP registration status for a content bundle
/// </summary>
public static class GetBundleIpStatusQueryHandler
{
    public static async Task<IpVerificationResponse?> Handle(
        GetBundleIpStatusQuery request,
        IContentBundleRepository repository,
        ILogger<GetBundleIpStatusQuery> logger,
        IOptions<StoryProtocolOptions> options,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.BundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(request.BundleId));
        }

        var bundle = await repository.GetByIdAsync(request.BundleId);

        if (bundle == null)
        {
            logger.LogWarning("Content bundle not found for IP status check: {BundleId}", request.BundleId);
            return null;
        }

        return MapToIpStatusResponse(bundle, options.Value);
    }

    private static IpVerificationResponse MapToIpStatusResponse(ContentBundle bundle, StoryProtocolOptions options)
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
                ? $"{options.ExplorerBaseUrl}/address/{storyProtocol.IpAssetId}"
                : null
        };
    }
}
