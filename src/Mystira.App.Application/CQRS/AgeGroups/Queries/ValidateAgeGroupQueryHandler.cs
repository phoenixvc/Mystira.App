using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Handler for validating if an age group value exists.
/// </summary>
public class ValidateAgeGroupQueryHandler : IQueryHandler<ValidateAgeGroupQuery, bool>
{
    private readonly IAgeGroupRepository _repository;
    private readonly ILogger<ValidateAgeGroupQueryHandler> _logger;

    public ValidateAgeGroupQueryHandler(
        IAgeGroupRepository repository,
        ILogger<ValidateAgeGroupQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ValidateAgeGroupQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating age group: {Value}", query.Value);
        var isValid = await _repository.ExistsByValueAsync(query.Value);
        _logger.LogInformation("Age group '{Value}' is {Status}", query.Value, isValid ? "valid" : "invalid");
        return isValid;
    }
}
