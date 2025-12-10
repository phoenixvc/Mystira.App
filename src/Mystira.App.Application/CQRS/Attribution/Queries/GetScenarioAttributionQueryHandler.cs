using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Helpers;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Attribution;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Handler for GetScenarioAttributionQuery - retrieves creator credits for a scenario
/// </summary>
public class GetScenarioAttributionQueryHandler : IQueryHandler<GetScenarioAttributionQuery, ContentAttributionResponse?>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenarioAttributionQueryHandler> _logger;

    public GetScenarioAttributionQueryHandler(
        IScenarioRepository repository,
        ILogger<GetScenarioAttributionQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ContentAttributionResponse?> Handle(GetScenarioAttributionQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(request.ScenarioId));
        }

        var scenario = await _repository.GetByIdAsync(request.ScenarioId);

        if (scenario == null)
        {
            _logger.LogWarning("Scenario not found for attribution: {ScenarioId}", request.ScenarioId);
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
            Credits = new List<CreatorCreditResponse>()
        };

        if (scenario.StoryProtocol?.Contributors != null)
        {
            foreach (var contributor in scenario.StoryProtocol.Contributors)
            {
                response.Credits.Add(new CreatorCreditResponse
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
