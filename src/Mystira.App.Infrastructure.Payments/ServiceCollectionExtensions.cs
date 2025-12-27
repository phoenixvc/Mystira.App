using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Application.Ports.Payments;
using Mystira.App.Infrastructure.Payments.Configuration;
using Mystira.App.Infrastructure.Payments.HealthChecks;
using Mystira.App.Infrastructure.Payments.Services.Mock;
using Mystira.App.Infrastructure.Payments.Services.PeachPayments;

namespace Mystira.App.Infrastructure.Payments;

/// <summary>
/// Extension methods for registering payment services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds payment services to the service collection.
    /// Provider selection is configuration-driven.
    /// </summary>
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var paymentSection = configuration.GetSection(PaymentOptions.SectionName);
        services.Configure<PaymentOptions>(paymentSection);

        var options = paymentSection.Get<PaymentOptions>() ?? new PaymentOptions();

        // Register appropriate implementation based on configuration
        if (!options.Enabled || options.UseMockImplementation)
        {
            // Use mock implementation for development/testing
            services.AddSingleton<IPaymentService, MockPaymentService>();
        }
        else
        {
            // Register real payment provider based on configuration
            switch (options.Provider)
            {
                case PaymentProvider.PeachPayments:
                    services.AddHttpClient<IPaymentService, PeachPaymentsService>(client =>
                    {
                        client.BaseAddress = new Uri(options.PeachPayments.BaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                    });
                    break;

                case PaymentProvider.Stripe:
                    // Future: services.AddScoped<IPaymentService, StripePaymentService>();
                    throw new NotImplementedException("Stripe payment provider not yet implemented");

                case PaymentProvider.PayFast:
                    // Future: services.AddScoped<IPaymentService, PayFastPaymentService>();
                    throw new NotImplementedException("PayFast payment provider not yet implemented");

                default:
                    throw new InvalidOperationException($"Unknown payment provider: {options.Provider}");
            }
        }

        return services;
    }

    /// <summary>
    /// Adds payment service health check.
    /// </summary>
    public static IHealthChecksBuilder AddPaymentServiceHealthCheck(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<PaymentServiceHealthCheck>(
            name ?? "payment_service",
            failureStatus ?? HealthStatus.Degraded,
            tags ?? new[] { "payments", "external", "ready" });
    }
}
