using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Handler for retrieving featured scenarios.
/// Returns scenarios marked as featured and active.
/// </summary>
public class GetFeaturedScenariosQueryHandler
    : IQueryHandler<GetFeaturedScenariosQuery, List<Scenario>>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetFeaturedScenariosQueryHandler> _logger;

    public GetFeaturedScenariosQueryHandler(
        IScenarioRepository repository,
        ILogger<GetFeaturedScenariosQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Scenario>> Handle(
        GetFeaturedScenariosQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving featured scenarios");

        // Get featured and active scenarios using direct LINQ query
        var scenarios = await _repository.GetQueryable()
            .Where(s => s.IsFeatured && s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} featured scenarios", scenarios.Count);

        return scenarios;
    }
}
