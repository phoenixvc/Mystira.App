namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Query to check application readiness (for Kubernetes/container orchestration).
/// Returns a simple status indicating the application is ready to receive traffic.
/// </summary>
public record GetReadinessQuery : IQuery<ProbeResult>;

/// <summary>
/// Result for readiness/liveness probes.
/// </summary>
public record ProbeResult(
    string Status,
    DateTime Timestamp
);
