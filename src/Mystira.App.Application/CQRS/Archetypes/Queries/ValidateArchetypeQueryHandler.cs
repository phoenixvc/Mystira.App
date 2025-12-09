using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Handler for validating if an archetype name exists.
/// </summary>
public class ValidateArchetypeQueryHandler : IQueryHandler<ValidateArchetypeQuery, bool>
{
    private readonly IArchetypeRepository _repository;
    private readonly ILogger<ValidateArchetypeQueryHandler> _logger;

    public ValidateArchetypeQueryHandler(
        IArchetypeRepository repository,
        ILogger<ValidateArchetypeQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ValidateArchetypeQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating archetype: {Name}", query.Name);
        var isValid = await _repository.ExistsByNameAsync(query.Name);
        _logger.LogInformation("Archetype '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
