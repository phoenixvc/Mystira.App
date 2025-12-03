using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Handler for retrieving an age group by ID.
/// </summary>
public class GetAgeGroupByIdQueryHandler : IQueryHandler<GetAgeGroupByIdQuery, AgeGroupDefinition?>
{
    private readonly IAgeGroupRepository _repository;
    private readonly ILogger<GetAgeGroupByIdQueryHandler> _logger;

    public GetAgeGroupByIdQueryHandler(
        IAgeGroupRepository repository,
        ILogger<GetAgeGroupByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AgeGroupDefinition?> Handle(
        GetAgeGroupByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving age group with id: {Id}", query.Id);
        var ageGroup = await _repository.GetByIdAsync(query.Id);
        
        if (ageGroup == null)
        {
            _logger.LogWarning("Age group with id {Id} not found", query.Id);
        }
        
        return ageGroup;
    }
}
