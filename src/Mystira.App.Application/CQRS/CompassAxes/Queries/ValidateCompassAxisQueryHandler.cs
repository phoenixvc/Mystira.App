using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Handler for validating if a compass axis name exists.
/// </summary>
public class ValidateCompassAxisQueryHandler : IQueryHandler<ValidateCompassAxisQuery, bool>
{
    private readonly ICompassAxisRepository _repository;
    private readonly ILogger<ValidateCompassAxisQueryHandler> _logger;

    public ValidateCompassAxisQueryHandler(
        ICompassAxisRepository repository,
        ILogger<ValidateCompassAxisQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ValidateCompassAxisQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating compass axis: {Name}", query.Name);
        var isValid = await _repository.ExistsByNameAsync(query.Name);
        _logger.LogInformation("Compass axis '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
