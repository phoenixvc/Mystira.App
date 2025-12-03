namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Query to check application liveness (for Kubernetes/container orchestration).
/// Returns a simple status indicating the application is alive and running.
/// </summary>
public record GetLivenessQuery : IQuery<ProbeResult>;
