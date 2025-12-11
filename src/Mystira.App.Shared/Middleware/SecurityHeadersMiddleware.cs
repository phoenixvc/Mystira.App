using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Mystira.App.Shared.Middleware;

/// <summary>
/// Options for configuring security headers.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// Use strict CSP (no unsafe-inline/unsafe-eval). Set to false for Blazor WASM apps.
    /// Default: true (strict mode for API-only services)
    /// </summary>
    public bool UseStrictCsp { get; set; } = true;

    /// <summary>
    /// Additional allowed script sources (e.g., CDNs).
    /// </summary>
    public string[] AdditionalScriptSources { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Additional allowed style sources.
    /// </summary>
    public string[] AdditionalStyleSources { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Additional allowed font sources.
    /// </summary>
    public string[] AdditionalFontSources { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Custom frame-ancestors value. Default: 'none' (no embedding).
    /// </summary>
    public string FrameAncestors { get; set; } = "'none'";
}

/// <summary>
/// Middleware to add OWASP-recommended security headers to all responses.
/// Helps prevent XSS, clickjacking, and other common web vulnerabilities.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions>? options = null)
    {
        _next = next;
        _options = options?.Value ?? new SecurityHeadersOptions();
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
        var cspPolicy = BuildCspPolicy();
        context.Response.Headers.Append("Content-Security-Policy", cspPolicy);

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

    private string BuildCspPolicy()
    {
        var scriptSrc = _options.UseStrictCsp
            ? "'self'" // Strict: no inline scripts
            : "'self' 'unsafe-inline' 'unsafe-eval'"; // Permissive: needed for Blazor WASM

        var styleSrc = _options.UseStrictCsp
            ? "'self'" // Strict: no inline styles
            : "'self' 'unsafe-inline'"; // Permissive: needed for Blazor WASM

        // Add additional sources if configured
        if (_options.AdditionalScriptSources.Length > 0)
        {
            scriptSrc += " " + string.Join(" ", _options.AdditionalScriptSources);
        }

        if (_options.AdditionalStyleSources.Length > 0)
        {
            styleSrc += " " + string.Join(" ", _options.AdditionalStyleSources);
        }

        // Fonts: start with 'self' and allow data: URIs and https; then append any explicit hosts
        var fontSrc = "'self' data: https:";
        if (_options.AdditionalFontSources.Length > 0)
        {
            fontSrc += " " + string.Join(" ", _options.AdditionalFontSources);
        }

        return $"default-src 'self'; " +
               $"script-src {scriptSrc}; " +
               $"style-src {styleSrc}; " +
               $"img-src 'self' data: https:; " +
               $"font-src {fontSrc}; " +
               $"connect-src 'self' https:; " +
               $"frame-ancestors {_options.FrameAncestors}";
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware with default strict CSP (recommended for APIs).
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Adds security headers middleware with custom options.
    /// Use this overload for Blazor WASM apps that need permissive CSP.
    /// </summary>
    /// <example>
    /// // For API-only (strict CSP - default):
    /// app.UseSecurityHeaders();
    ///
    /// // For Blazor WASM (permissive CSP):
    /// app.UseSecurityHeaders(options => options.UseStrictCsp = false);
    /// </example>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder builder,
        Action<SecurityHeadersOptions> configureOptions)
    {
        var options = new SecurityHeadersOptions();
        configureOptions(options);
        return builder.UseMiddleware<SecurityHeadersMiddleware>(Options.Create(options));
    }
}
