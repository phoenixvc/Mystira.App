using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Handler for retrieving an archetype by ID.
/// </summary>
public class GetArchetypeByIdQueryHandler : IQueryHandler<GetArchetypeByIdQuery, ArchetypeDefinition?>
{
    private readonly IArchetypeRepository _repository;
    private readonly ILogger<GetArchetypeByIdQueryHandler> _logger;

    public GetArchetypeByIdQueryHandler(
        IArchetypeRepository repository,
        ILogger<GetArchetypeByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ArchetypeDefinition?> Handle(
        GetArchetypeByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving archetype with id: {Id}", query.Id);
        var archetype = await _repository.GetByIdAsync(query.Id);

        if (archetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", query.Id);
        }

        return archetype;
    }
}
