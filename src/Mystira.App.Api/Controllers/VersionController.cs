using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using System.Reflection;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Provides version information about the API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<VersionController> _logger;

    public VersionController(IWebHostEnvironment environment, ILogger<VersionController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Get the version information of the API
    /// </summary>
    /// <returns>Version information including version number, API name, build date, and environment</returns>
    [HttpGet]
    public ActionResult<VersionInfo> GetVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
            var buildDate = GetBuildDate(assembly);

            var versionInfo = new VersionInfo
            {
                Version = version,
                ApiName = "Mystira.App.Api",
                BuildDate = buildDate,
                Environment = _environment.EnvironmentName
            };

            _logger.LogDebug("Version information requested: {Version}", version);

            return Ok(versionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version information");
            return StatusCode(500, new { error = "Unable to retrieve version information" });
        }
    }

    private static string GetBuildDate(Assembly assembly)
    {
        // Get the build date from the assembly's creation time
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
        {
            var fileInfo = new System.IO.FileInfo(location);
            return fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    }
}
