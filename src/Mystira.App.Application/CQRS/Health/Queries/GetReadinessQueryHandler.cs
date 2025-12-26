using Microsoft.Extensions.Logging;

namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Wolverine message handler for readiness probe.
/// Simple check indicating application is ready to receive traffic.
/// </summary>
public static class GetReadinessQueryHandler
{
    public static Task<ProbeResult> Handle(
        GetReadinessQuery request,
        ILogger<GetReadinessQuery> logger,
        CancellationToken ct)
    {
        logger.LogDebug("Readiness probe checked");

        return Task.FromResult(new ProbeResult(
            Status: "ready",
            Timestamp: DateTime.UtcNow
        ));
    }
}
