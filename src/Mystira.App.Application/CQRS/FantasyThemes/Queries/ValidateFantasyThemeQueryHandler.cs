using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Handler for validating if a fantasy theme name exists.
/// </summary>
public class ValidateFantasyThemeQueryHandler : IQueryHandler<ValidateFantasyThemeQuery, bool>
{
    private readonly IFantasyThemeRepository _repository;
    private readonly ILogger<ValidateFantasyThemeQueryHandler> _logger;

    public ValidateFantasyThemeQueryHandler(
        IFantasyThemeRepository repository,
        ILogger<ValidateFantasyThemeQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ValidateFantasyThemeQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating fantasy theme: {Name}", query.Name);
        var isValid = await _repository.ExistsByNameAsync(query.Name);
        _logger.LogInformation("Fantasy theme '{Name}' is {Status}", query.Name, isValid ? "valid" : "invalid");
        return isValid;
    }
}
