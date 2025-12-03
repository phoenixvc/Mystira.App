using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Handler for retrieving all archetypes.
/// </summary>
public class GetAllArchetypesQueryHandler : IQueryHandler<GetAllArchetypesQuery, List<ArchetypeDefinition>>
{
    private readonly IArchetypeRepository _repository;
    private readonly ILogger<GetAllArchetypesQueryHandler> _logger;

    public GetAllArchetypesQueryHandler(
        IArchetypeRepository repository,
        ILogger<GetAllArchetypesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ArchetypeDefinition>> Handle(
        GetAllArchetypesQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all archetypes");
        var archetypes = await _repository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} archetypes", archetypes.Count);
        return archetypes;
    }
}
