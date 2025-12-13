using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Mystira.App.Admin.Web.Services;
using Mystira.App.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Data Protection (shared between Admin.Web and Admin.Api so auth cookies work across apps)
var sharedKeysPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", ".aspnet-keys"));
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeysPath))
    .SetApplicationName("Mystira.Admin");

builder.Services
    .AddControllersWithViews();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IAppStatusService, AppStatusService>();

builder.Services
    .AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "Mystira.Admin.Auth";
        options.Cookie.HttpOnly = true;

        var isDev = builder.Environment.IsDevelopment();
        options.Cookie.SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.Strict;
        options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.None : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        options.LoginPath = "/admin/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/admin/forbidden";
    });

builder.Services.AddAuthorization();

// Reverse proxy: forward /api/** requests to Mystira.App.Admin.Api
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/admin/error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Security headers (CSP nonce used in Razor views)
if (app.Environment.IsDevelopment())
{
    app.UseSecurityHeaders(options =>
    {
        options.UseStrictCsp = false;
        options.AdditionalScriptSources = new[]
        {
            "https://cdn.jsdelivr.net",
            "https://cdnjs.cloudflare.com",
            "https://code.jquery.com"
        };
        options.AdditionalStyleSources = new[]
        {
            "https://cdn.jsdelivr.net",
            "https://cdnjs.cloudflare.com"
        };
        options.AdditionalFontSources = new[]
        {
            "https://cdnjs.cloudflare.com",
            "https://cdn.jsdelivr.net",
            "https://fonts.gstatic.com"
        };
    });
}
else
{
    app.UseSecurityHeaders(options =>
    {
        options.UseStrictCsp = true;
        options.UseNonce = true;
        options.AdditionalScriptSources = new[]
        {
            "https://cdn.jsdelivr.net",
            "https://cdnjs.cloudflare.com",
            "https://code.jquery.com"
        };
        options.AdditionalStyleSources = new[]
        {
            "https://cdn.jsdelivr.net",
            "https://cdnjs.cloudflare.com"
        };
        options.AdditionalFontSources = new[]
        {
            "https://cdnjs.cloudflare.com",
            "https://cdn.jsdelivr.net",
            "https://fonts.gstatic.com"
        };
    });
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Keep proxy mapping after controllers so local routes (e.g. /api/auth/**) win.
app.MapReverseProxy();

app.Run();

namespace Mystira.App.Admin.Web
{
    public class Program { }
}
