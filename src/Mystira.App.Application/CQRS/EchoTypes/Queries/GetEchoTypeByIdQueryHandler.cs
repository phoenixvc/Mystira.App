using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Handler for retrieving an echo type by ID.
/// </summary>
public class GetEchoTypeByIdQueryHandler : IQueryHandler<GetEchoTypeByIdQuery, EchoTypeDefinition?>
{
    private readonly IEchoTypeRepository _repository;
    private readonly ILogger<GetEchoTypeByIdQueryHandler> _logger;

    public GetEchoTypeByIdQueryHandler(
        IEchoTypeRepository repository,
        ILogger<GetEchoTypeByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<EchoTypeDefinition?> Handle(
        GetEchoTypeByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving echo type with id: {Id}", query.Id);
        var echoType = await _repository.GetByIdAsync(query.Id);

        if (echoType == null)
        {
            _logger.LogWarning("Echo type with id {Id} not found", query.Id);
        }

        return echoType;
    }
}
