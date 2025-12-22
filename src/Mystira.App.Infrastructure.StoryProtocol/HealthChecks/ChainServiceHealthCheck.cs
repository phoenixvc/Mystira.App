using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Configuration.StoryProtocol;
using Mystira.App.Infrastructure.StoryProtocol.Services;

namespace Mystira.App.Infrastructure.StoryProtocol.HealthChecks;

/// <summary>
/// Health check for the Mystira.Chain gRPC service connection.
/// Verifies that the gRPC channel can communicate with the Python Chain service.
/// </summary>
public class ChainServiceHealthCheck : IHealthCheck
{
    private readonly GrpcChainServiceAdapter? _grpcAdapter;
    private readonly ChainServiceOptions _options;
    private readonly ILogger<ChainServiceHealthCheck> _logger;

    public ChainServiceHealthCheck(
        IOptions<ChainServiceOptions> options,
        ILogger<ChainServiceHealthCheck> logger,
        GrpcChainServiceAdapter? grpcAdapter = null)
    {
        _options = options.Value;
        _logger = logger;
        _grpcAdapter = grpcAdapter;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // If gRPC is not enabled, skip the health check
        if (!_options.UseGrpc)
        {
            return HealthCheckResult.Healthy(
                "Chain service gRPC is disabled (using mock or direct implementation)",
                data: new Dictionary<string, object>
                {
                    ["UseGrpc"] = false,
                    ["Mode"] = "mock_or_direct"
                });
        }

        // If health checks are disabled in config, skip
        if (!_options.EnableHealthChecks)
        {
            return HealthCheckResult.Healthy(
                "Chain service health checks are disabled",
                data: new Dictionary<string, object>
                {
                    ["HealthChecksEnabled"] = false
                });
        }

        // If adapter is not available (shouldn't happen if DI is correct)
        if (_grpcAdapter == null)
        {
            return HealthCheckResult.Degraded(
                "Chain service gRPC adapter is not available",
                data: new Dictionary<string, object>
                {
                    ["UseGrpc"] = true,
                    ["AdapterAvailable"] = false
                });
        }

        try
        {
            var isHealthy = await _grpcAdapter.IsHealthyAsync();

            if (isHealthy)
            {
                _logger.LogDebug("Chain service health check passed");
                return HealthCheckResult.Healthy(
                    "Chain service gRPC connection is healthy",
                    data: new Dictionary<string, object>
                    {
                        ["Endpoint"] = _options.GrpcEndpoint,
                        ["Status"] = "serving"
                    });
            }
            else
            {
                _logger.LogWarning("Chain service is not serving");
                return HealthCheckResult.Unhealthy(
                    "Chain service is not serving",
                    data: new Dictionary<string, object>
                    {
                        ["Endpoint"] = _options.GrpcEndpoint,
                        ["Status"] = "not_serving"
                    });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chain service health check failed");
            return HealthCheckResult.Unhealthy(
                "Chain service health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["Endpoint"] = _options.GrpcEndpoint,
                    ["Error"] = ex.Message
                });
        }
    }
}
