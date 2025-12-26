using Microsoft.Extensions.Logging;

namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Wolverine message handler for liveness probe.
/// Simple check indicating application is alive and running.
/// </summary>
public static class GetLivenessQueryHandler
{
    public static Task<ProbeResult> Handle(
        GetLivenessQuery request,
        ILogger<GetLivenessQuery> logger,
        CancellationToken ct)
    {
        logger.LogDebug("Liveness probe checked");

        return Task.FromResult(new ProbeResult(
            Status: "alive",
            Timestamp: DateTime.UtcNow
        ));
    }
}
