using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Domain.Exceptions;

namespace Mystira.App.Api.Middleware;

/// <summary>
/// Global exception handler that converts domain exceptions to RFC 7807 Problem Details responses.
/// Registered via AddExceptionHandler<GlobalExceptionHandler>().
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            NotFoundException notFound => CreateProblemDetails(
                httpContext,
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFound.Message,
                notFound.ErrorCode,
                notFound.Details),

            ValidationException validation => CreateProblemDetails(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                validation.Message,
                validation.ErrorCode,
                new Dictionary<string, object>
                {
                    ["errors"] = validation.Errors.Select(e => new { e.Field, e.Message })
                }),

            ConflictException conflict => CreateProblemDetails(
                httpContext,
                StatusCodes.Status409Conflict,
                "Resource Conflict",
                conflict.Message,
                conflict.ErrorCode,
                conflict.Details),

            ForbiddenException forbidden => CreateProblemDetails(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access Forbidden",
                forbidden.Message,
                forbidden.ErrorCode,
                forbidden.Details),

            BusinessRuleException businessRule => CreateProblemDetails(
                httpContext,
                StatusCodes.Status422UnprocessableEntity,
                "Business Rule Violation",
                businessRule.Message,
                businessRule.ErrorCode,
                businessRule.Details),

            DomainException domain => CreateProblemDetails(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Domain Error",
                domain.Message,
                domain.ErrorCode,
                domain.Details),

            ArgumentException argument => CreateProblemDetails(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Invalid Argument",
                argument.Message,
                "INVALID_ARGUMENT"),

            InvalidOperationException invalidOp => CreateProblemDetails(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Invalid Operation",
                invalidOp.Message,
                "INVALID_OPERATION"),

            UnauthorizedAccessException => CreateProblemDetails(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                "UNAUTHORIZED"),

            _ => null
        };

        if (problemDetails is null)
        {
            // Log unhandled exceptions
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            // Create generic 500 response
            problemDetails = CreateProblemDetails(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.",
                "INTERNAL_ERROR");

            // Include stack trace in development
            if (_environment.IsDevelopment())
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }
        }
        else
        {
            // Log handled domain exceptions at appropriate level
            LogException(exception, problemDetails.Status);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string errorCode,
        IDictionary<string, object>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");

        if (extensions != null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return problemDetails;
    }

    private void LogException(Exception exception, int? statusCode)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, exception, "Domain exception: {ExceptionType} - {Message}",
            exception.GetType().Name, exception.Message);
    }
}
