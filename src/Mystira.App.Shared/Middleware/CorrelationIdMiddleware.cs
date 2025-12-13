using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Mystira.App.Shared.Middleware;

/// <summary>
/// Middleware that extracts or generates a correlation ID for each request.
/// The correlation ID is used for distributed tracing across services.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// The header name used for correlation ID propagation.
    /// Uses the W3C standard traceparent header name.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// Alternative header names that may contain correlation IDs from upstream services.
    /// </summary>
    public static readonly string[] AlternativeHeaders = new[]
    {
        "X-Request-Id",
        "Request-Id",
        "traceparent"
    };

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request headers
        var correlationId = GetCorrelationIdFromRequest(context.Request);

        // If not found, generate a new one
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = GenerateCorrelationId();
        }

        // Store in HttpContext.Items for access throughout the request
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers for client correlation
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        // Push correlation ID to Serilog context for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Also log the correlation ID at trace level for debugging
            _logger.LogTrace("Request started with CorrelationId: {CorrelationId}", correlationId);

            await _next(context);
        }
    }

    private static string? GetCorrelationIdFromRequest(HttpRequest request)
    {
        // Check primary header first
        if (request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Check alternative headers using LINQ for cleaner filtering
        var foundCorrelationId = AlternativeHeaders
            .Select(header =>
            {
                if (request.Headers.TryGetValue(header, out var altCorrelationId) &&
                    !string.IsNullOrWhiteSpace(altCorrelationId))
                {
                    // For traceparent, extract the trace-id portion (second segment)
                    var value = altCorrelationId.ToString();
                    if (header == "traceparent" && value.Contains('-'))
                    {
                        var parts = value.Split('-');
                        if (parts.Length >= 2)
                        {
                            return parts[1]; // Return the trace-id portion
                        }
                    }
                    return value;
                }
                return null;
            })
            .FirstOrDefault(val => !string.IsNullOrWhiteSpace(val));

        return foundCorrelationId;
    }

    private static string GenerateCorrelationId()
    {
        // Use full GUID for correlation IDs to maintain 128-bit uniqueness
        // The "N" format produces 32 hex chars without hyphens
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods for adding CorrelationIdMiddleware to the pipeline.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the application pipeline.
    /// Should be added early in the pipeline before other middleware.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
