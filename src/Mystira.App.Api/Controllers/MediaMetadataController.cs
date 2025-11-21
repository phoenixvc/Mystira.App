using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaMetadataController : ControllerBase
{
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaMetadataController> _logger;

    public MediaMetadataController(IMediaMetadataService mediaMetadataService, ILogger<MediaMetadataController> logger)
    {
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the media metadata file
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MediaMetadataFile>> GetMediaMetadataFile()
    {
        try
        {
            var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
            return Ok(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media metadata file");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting media metadata file",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
