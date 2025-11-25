using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Mystira.App.Shared.Middleware;

/// <summary>
/// Middleware to add OWASP-recommended security headers to all responses.
/// Helps prevent XSS, clickjacking, and other common web vulnerabilities.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME-type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Prevents clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Legacy XSS filter (mostly superseded by CSP)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy: Controls referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content-Security-Policy: Helps prevent XSS and data injection attacks
        // Note: Adjust for your specific needs. This is a restrictive starting point.
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // Blazor WASM requires unsafe-eval
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self' https:; " +
            "frame-ancestors 'none'");

        // Permissions-Policy: Controls browser features
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=()");

        // Strict-Transport-Security: Enforce HTTPS (only add in production with HTTPS)
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
