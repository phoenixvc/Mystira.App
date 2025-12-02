using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Handler for retrieving all echo types.
/// </summary>
public class GetAllEchoTypesQueryHandler : IQueryHandler<GetAllEchoTypesQuery, List<EchoTypeDefinition>>
{
    private readonly IEchoTypeRepository _repository;
    private readonly ILogger<GetAllEchoTypesQueryHandler> _logger;

    public GetAllEchoTypesQueryHandler(
        IEchoTypeRepository repository,
        ILogger<GetAllEchoTypesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<EchoTypeDefinition>> Handle(
        GetAllEchoTypesQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all echo types");
        var echoTypes = await _repository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} echo types", echoTypes.Count);
        return echoTypes;
    }
}
