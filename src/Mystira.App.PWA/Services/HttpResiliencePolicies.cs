using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Provides resilience policies for HTTP clients using Polly.
/// Includes retry, circuit breaker, and timeout policies.
/// </summary>
public static class HttpResiliencePolicies
{
    /// <summary>
    /// Gets a retry policy with exponential backoff.
    /// Retries on transient HTTP errors (5xx, 408) and network failures.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger? logger = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles HttpRequestException, 5xx, 408
            .Or<TimeoutRejectedException>() // Also retry on timeout
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger?.LogWarning(
                        "Retry {RetryAttempt} after {Delay}s due to {Reason}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Gets a circuit breaker policy.
    /// Opens the circuit after consecutive failures, preventing further requests.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger? logger = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    logger?.LogWarning(
                        "Circuit breaker opened for {BreakDelay}s due to {Reason}",
                        breakDelay.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    logger?.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    logger?.LogInformation("Circuit breaker half-open, testing...");
                });
    }

    /// <summary>
    /// Gets a timeout policy.
    /// Cancels requests that take too long.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutSeconds = 30)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Optimistic);
    }

    /// <summary>
    /// Gets a combined policy wrap with retry, circuit breaker, and timeout.
    /// Order: Timeout -> Retry -> Circuit Breaker
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger? logger = null, int timeoutSeconds = 30)
    {
        // Policies are applied from outermost to innermost:
        // 1. Timeout (outermost) - overall request timeout
        // 2. Retry - retry on failures
        // 3. Circuit breaker (innermost) - track failures and break
        return Policy.WrapAsync(
            GetTimeoutPolicy(timeoutSeconds),
            GetRetryPolicy(logger),
            GetCircuitBreakerPolicy(logger));
    }

    /// <summary>
    /// Extension method to add combined resilience policies to an HttpClient builder.
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(
        this IHttpClientBuilder builder,
        ILogger? logger = null,
        int timeoutSeconds = 30)
    {
        return builder
            .AddPolicyHandler(GetRetryPolicy(logger))
            .AddPolicyHandler(GetCircuitBreakerPolicy(logger))
            .AddPolicyHandler(GetTimeoutPolicy(timeoutSeconds));
    }
}
