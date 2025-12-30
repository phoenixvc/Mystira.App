using Microsoft.Extensions.Logging;
using Mystira.App.Application.Helpers;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Attribution;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Wolverine handler for GetScenarioAttributionQuery - retrieves creator credits for a scenario
/// </summary>
public static class GetScenarioAttributionQueryHandler
{
    public static async Task<ContentAttributionResponse?> Handle(
        GetScenarioAttributionQuery request,
        IScenarioRepository repository,
        ILogger<GetScenarioAttributionQuery> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(request.ScenarioId));
        }

        var scenario = await repository.GetByIdAsync(request.ScenarioId);

        if (scenario == null)
        {
            logger.LogWarning("Scenario not found for attribution: {ScenarioId}", request.ScenarioId);
            return null;
        }

        return MapToAttributionResponse(scenario);
    }

    private static ContentAttributionResponse MapToAttributionResponse(Scenario scenario)
    {
        var response = new ContentAttributionResponse
        {
            ContentId = scenario.Id,
            ContentTitle = scenario.Title,
            IsIpRegistered = scenario.StoryProtocol?.IsRegistered ?? false,
            IpAssetId = scenario.StoryProtocol?.IpAssetId,
            RegisteredAt = scenario.StoryProtocol?.RegisteredAt,
            Credits = new List<CreatorCredit>()
        };

        if (scenario.StoryProtocol?.Contributors != null)
        {
            foreach (var contributor in scenario.StoryProtocol.Contributors)
            {
                response.Credits.Add(new CreatorCredit
                {
                    Name = contributor.Name,
                    Role = ContributorHelpers.GetRoleDisplayName(contributor.Role),
                    ContributionPercentage = contributor.ContributionPercentage
                });
            }
        }

        return response;
    }
}
