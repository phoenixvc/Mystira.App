using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mystira.App.Shared.Middleware;

/// <summary>
/// Options for configuring request logging behavior.
/// </summary>
public class RequestLoggingOptions
{
    /// <summary>
    /// Log request bodies for these HTTP methods. Default: POST, PUT, PATCH.
    /// </summary>
    public string[] LogBodyForMethods { get; set; } = { "POST", "PUT", "PATCH" };

    /// <summary>
    /// Maximum body size to log (in bytes). Default: 4096.
    /// </summary>
    public int MaxBodyLength { get; set; } = 4096;

    /// <summary>
    /// Request paths to exclude from logging (e.g., health checks).
    /// </summary>
    public string[] ExcludedPaths { get; set; } = { "/health", "/health/ready", "/health/live", "/swagger" };

    /// <summary>
    /// Whether to log request headers. Default: false (security consideration).
    /// </summary>
    public bool LogHeaders { get; set; } = false;

    /// <summary>
    /// Headers to redact from logs (when LogHeaders is true).
    /// </summary>
    public string[] RedactedHeaders { get; set; } = { "Authorization", "Cookie", "X-Api-Key" };

    /// <summary>
    /// Response time threshold (ms) to log as warning. Default: 3000.
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 3000;
}

/// <summary>
/// Middleware that logs detailed request and response information for observability.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestLoggingOptions _options;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IOptions<RequestLoggingOptions>? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new RequestLoggingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for excluded paths
        var path = context.Request.Path.Value ?? string.Empty;
        if (_options.ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        // Log request start
        LogRequestStart(context, requestId);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogRequestException(context, requestId, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }

        stopwatch.Stop();

        // Log request completion
        LogRequestCompletion(context, requestId, stopwatch.ElapsedMilliseconds);
    }

    private void LogRequestStart(HttpContext context, string requestId)
    {
        var request = context.Request;

        _logger.LogInformation(
            "Request started: {Method} {Path}{QueryString} | RequestId: {RequestId} | Client: {ClientIp}",
            request.Method,
            request.Path,
            request.QueryString,
            requestId,
            GetClientIp(context));
    }

    private void LogRequestCompletion(HttpContext context, string requestId, long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var logLevel = GetLogLevelForStatusCode(statusCode, elapsedMs);

        _logger.Log(
            logLevel,
            "Request completed: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | RequestId: {RequestId}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            elapsedMs,
            requestId);

        // Log slow requests as warnings
        if (elapsedMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} took {Duration}ms (threshold: {Threshold}ms) | RequestId: {RequestId}",
                context.Request.Method,
                context.Request.Path,
                elapsedMs,
                _options.SlowRequestThresholdMs,
                requestId);
        }
    }

    private void LogRequestException(HttpContext context, string requestId, long elapsedMs, Exception ex)
    {
        _logger.LogError(
            ex,
            "Request failed: {Method} {Path} | Duration: {Duration}ms | RequestId: {RequestId} | Exception: {ExceptionType}",
            context.Request.Method,
            context.Request.Path,
            elapsedMs,
            requestId,
            ex.GetType().Name);
    }

    private LogLevel GetLogLevelForStatusCode(int statusCode, long elapsedMs)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ when elapsedMs > _options.SlowRequestThresholdMs => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }

    private static string GetClientIp(HttpContext context)
    {
        // Try to get the real client IP from forwarded headers (when behind a proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs; the first is the client
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extension methods for adding RequestLoggingMiddleware to the pipeline.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds request logging middleware to the application pipeline.
    /// Should be added after UseCorrelationId for proper correlation ID logging.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Adds request logging middleware with custom options.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(
        this IApplicationBuilder builder,
        Action<RequestLoggingOptions> configureOptions)
    {
        var options = new RequestLoggingOptions();
        configureOptions(options);
        return builder.UseMiddleware<RequestLoggingMiddleware>(Options.Create(options));
    }
}
