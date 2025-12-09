using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Handler for validating if an echo type name exists.
/// </summary>
public class ValidateEchoTypeQueryHandler : IQueryHandler<ValidateEchoTypeQuery, bool>
{
    private readonly IEchoTypeRepository _repository;
    private readonly ILogger<ValidateEchoTypeQueryHandler> _logger;

    public ValidateEchoTypeQueryHandler(
        IEchoTypeRepository repository,
        ILogger<ValidateEchoTypeQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ValidateEchoTypeQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating echo type: {Name}", query.Name);
        var isValid = await _repository.ExistsByNameAsync(query.Name);
        _logger.LogInformation("Echo type '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
