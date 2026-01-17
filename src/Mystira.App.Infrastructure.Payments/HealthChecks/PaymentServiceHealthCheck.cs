using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Payments;
using Mystira.App.Infrastructure.Payments.Configuration;

namespace Mystira.App.Infrastructure.Payments.HealthChecks;

/// <summary>
/// Health check for payment service connectivity.
/// </summary>
public class PaymentServiceHealthCheck : IHealthCheck
{
    private readonly IPaymentService _paymentService;
    private readonly PaymentOptions _options;
    private readonly ILogger<PaymentServiceHealthCheck> _logger;

    public PaymentServiceHealthCheck(
        IPaymentService paymentService,
        IOptions<PaymentOptions> options,
        ILogger<PaymentServiceHealthCheck> logger)
    {
        _paymentService = paymentService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.Enabled)
            {
                return HealthCheckResult.Healthy("Payment service is disabled by configuration");
            }

            if (_options.UseMockImplementation)
            {
                return HealthCheckResult.Healthy("Payment service is using mock implementation");
            }

            var isHealthy = await _paymentService.IsHealthyAsync();

            if (isHealthy)
            {
                return HealthCheckResult.Healthy($"Payment service ({_options.Provider}) is healthy");
            }

            _logger.LogWarning("Payment service health check failed");
            return HealthCheckResult.Degraded($"Payment service ({_options.Provider}) is not responding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment service health check threw an exception");
            return HealthCheckResult.Unhealthy(
                $"Payment service ({_options.Provider}) health check failed",
                ex);
        }
    }
}
