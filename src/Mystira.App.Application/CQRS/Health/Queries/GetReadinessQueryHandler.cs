using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Handler for readiness probe.
/// Simple check indicating application is ready to receive traffic.
/// </summary>
public class GetReadinessQueryHandler
    : IQueryHandler<GetReadinessQuery, ProbeResult>
{
    private readonly ILogger<GetReadinessQueryHandler> _logger;

    public GetReadinessQueryHandler(ILogger<GetReadinessQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<ProbeResult> Handle(
        GetReadinessQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Readiness probe checked");

        return Task.FromResult(new ProbeResult(
            Status: "ready",
            Timestamp: DateTime.UtcNow
        ));
    }
}
