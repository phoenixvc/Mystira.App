using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Attribution;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Handler for GetScenarioIpStatusQuery - retrieves IP registration status for a scenario
/// </summary>
public class GetScenarioIpStatusQueryHandler : IQueryHandler<GetScenarioIpStatusQuery, IpVerificationResponse?>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenarioIpStatusQueryHandler> _logger;
    private const string StoryProtocolExplorerBaseUrl = "https://explorer.story.foundation/ipa";

    public GetScenarioIpStatusQueryHandler(
        IScenarioRepository repository,
        ILogger<GetScenarioIpStatusQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IpVerificationResponse?> Handle(GetScenarioIpStatusQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(request.ScenarioId));
        }

        var scenario = await _repository.GetByIdAsync(request.ScenarioId);

        if (scenario == null)
        {
            _logger.LogWarning("Scenario not found for IP status check: {ScenarioId}", request.ScenarioId);
            return null;
        }

        return MapToIpStatusResponse(scenario);
    }

    private static IpVerificationResponse MapToIpStatusResponse(Scenario scenario)
    {
        var storyProtocol = scenario.StoryProtocol;
        var isRegistered = storyProtocol?.IsRegistered ?? false;

        return new IpVerificationResponse
        {
            ContentId = scenario.Id,
            ContentTitle = scenario.Title,
            IsRegistered = isRegistered,
            IpAssetId = storyProtocol?.IpAssetId,
            RegisteredAt = storyProtocol?.RegisteredAt,
            RegistrationTxHash = storyProtocol?.RegistrationTxHash,
            RoyaltyModuleId = storyProtocol?.RoyaltyModuleId,
            ContributorCount = storyProtocol?.Contributors?.Count ?? 0,
            ExplorerUrl = isRegistered && !string.IsNullOrEmpty(storyProtocol?.IpAssetId)
                ? $"{StoryProtocolExplorerBaseUrl}/{storyProtocol.IpAssetId}"
                : null
        };
    }
}
