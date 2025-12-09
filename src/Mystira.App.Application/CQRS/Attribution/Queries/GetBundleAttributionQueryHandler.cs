using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Attribution;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Handler for GetBundleAttributionQuery - retrieves creator credits for a content bundle
/// </summary>
public class GetBundleAttributionQueryHandler : IQueryHandler<GetBundleAttributionQuery, ContentAttributionResponse?>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetBundleAttributionQueryHandler> _logger;

    public GetBundleAttributionQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetBundleAttributionQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ContentAttributionResponse?> Handle(GetBundleAttributionQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(request.BundleId));
        }

        var bundle = await _repository.GetByIdAsync(request.BundleId);

        if (bundle == null)
        {
            _logger.LogWarning("Content bundle not found for attribution: {BundleId}", request.BundleId);
            return null;
        }

        return MapToAttributionResponse(bundle);
    }

    private static ContentAttributionResponse MapToAttributionResponse(ContentBundle bundle)
    {
        var response = new ContentAttributionResponse
        {
            ContentId = bundle.Id,
            ContentTitle = bundle.Title,
            IsIpRegistered = bundle.StoryProtocol?.IsRegistered ?? false,
            IpAssetId = bundle.StoryProtocol?.IpAssetId,
            RegisteredAt = bundle.StoryProtocol?.RegisteredAt,
            Credits = new List<CreatorCreditResponse>()
        };

        if (bundle.StoryProtocol?.Contributors != null)
        {
            foreach (var contributor in bundle.StoryProtocol.Contributors)
            {
                response.Credits.Add(new CreatorCreditResponse
                {
                    Name = contributor.Name,
                    Role = GetRoleDisplayName(contributor.Role),
                    ContributionPercentage = contributor.ContributionPercentage
                });
            }
        }

        return response;
    }

    private static string GetRoleDisplayName(ContributorRole role)
    {
        return role switch
        {
            ContributorRole.Writer => "Writer",
            ContributorRole.Artist => "Artist",
            ContributorRole.VoiceActor => "Voice Actor",
            ContributorRole.MusicComposer => "Music Composer",
            ContributorRole.SoundDesigner => "Sound Designer",
            ContributorRole.Editor => "Editor",
            ContributorRole.GameDesigner => "Game Designer",
            ContributorRole.QualityAssurance => "Quality Assurance",
            ContributorRole.Other => "Contributor",
            _ => "Contributor"
        };
    }
}
