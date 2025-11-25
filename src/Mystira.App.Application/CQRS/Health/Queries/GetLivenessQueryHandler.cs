using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Handler for liveness probe.
/// Simple check indicating application is alive and running.
/// </summary>
public class GetLivenessQueryHandler
    : IQueryHandler<GetLivenessQuery, ProbeResult>
{
    private readonly ILogger<GetLivenessQueryHandler> _logger;

    public GetLivenessQueryHandler(ILogger<GetLivenessQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<ProbeResult> Handle(
        GetLivenessQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Liveness probe checked");

        return Task.FromResult(new ProbeResult(
            Status: "alive",
            Timestamp: DateTime.UtcNow
        ));
    }
}
