using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
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

        // Read and log request body if configured
        string? requestBody = null;
        if (ShouldLogBody(context.Request))
        {
            requestBody = await ReadRequestBodyAsync(context.Request);
        }

        // Log request start
        LogRequestStart(context, requestId, requestBody);

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

    private bool ShouldLogBody(HttpRequest request)
    {
        // Only log body for configured methods
        if (!_options.LogBodyForMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Don't log if no content
        if (!request.ContentLength.HasValue || request.ContentLength == 0)
        {
            return false;
        }

        // Don't log file uploads or multipart
        var contentType = request.ContentType ?? string.Empty;
        if (contentType.Contains("multipart/form-data") || contentType.Contains("application/octet-stream"))
        {
            return false;
        }

        return true;
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            // Enable buffering so the body can be read multiple times
            request.EnableBuffering();

            // Read the body
            using var reader = new StreamReader(
                request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            // Reset the position so downstream handlers can read the body
            request.Body.Position = 0;

            // Truncate if too long
            if (body.Length > _options.MaxBodyLength)
            {
                return body.Substring(0, _options.MaxBodyLength) + $"... [truncated, {body.Length} total bytes]";
            }

            // Redact sensitive fields (basic implementation)
            return RedactSensitiveData(body);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Failed to read request body for logging (I/O error)");
            return null;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogDebug(ex, "Failed to read request body for logging (stream disposed)");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "Failed to read request body for logging (invalid operation)");
            return null;
        }
    }

    private static string RedactSensitiveData(string body)
    {
        // Comprehensive list of JSON field patterns to redact (case-insensitive)
        // Includes various naming conventions: camelCase, snake_case, kebab-case, PascalCase
        string[] sensitivePatterns = {
            // Passwords
            "password", "passwd", "pwd", "pass", "current_password", "new_password", "confirm_password",
            // Tokens and keys
            "token", "access_token", "refresh_token", "id_token", "bearer", "jwt",
            "api_key", "apiKey", "api-key", "apikey",
            "secret", "secret_key", "secretKey", "client_secret", "clientSecret",
            "private_key", "privateKey", "signing_key", "signingKey",
            // Auth headers
            "authorization", "auth", "auth_header", "x-api-key",
            // Credentials
            "credential", "credentials", "cred",
            // Financial
            "credit_card", "creditCard", "card_number", "cardNumber", "cvv", "cvc", "ccv",
            "account_number", "accountNumber", "routing_number", "routingNumber",
            // Personal identifiers
            "ssn", "social_security", "socialSecurity", "tax_id", "taxId",
            // Connection strings
            "connection_string", "connectionString", "conn_str", "connStr"
        };

        foreach (var pattern in sensitivePatterns)
        {
            // Redact JSON string values: "password": "value" -> "password": "[REDACTED]"
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                $@"(""{pattern}""\\s*:\\s*"")[^""]*("")",
                $"$1[REDACTED]$2",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return body;
    }

    private void LogRequestStart(HttpContext context, string requestId, string? requestBody = null)
    {
        var request = context.Request;

        if (!string.IsNullOrEmpty(requestBody))
        {
            _logger.LogInformation(
                "Request started: {Method} {Path}{QueryString} | RequestId: {RequestId} | Client: {ClientIp} | Body: {RequestBody}",
                request.Method,
                request.Path,
                request.QueryString,
                requestId,
                GetClientIp(context),
                requestBody);
        }
        else
        {
            _logger.LogInformation(
                "Request started: {Method} {Path}{QueryString} | RequestId: {RequestId} | Client: {ClientIp}",
                request.Method,
                request.Path,
                request.QueryString,
                requestId,
                GetClientIp(context));
        }
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
