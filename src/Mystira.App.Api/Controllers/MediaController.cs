using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;
using ErrorResponse = Mystira.App.Contracts.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaController : ControllerBase
{
    private readonly IMediaApiService _mediaService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediaApiService mediaService, IMediaMetadataService mediaMetadataService, ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a specific media asset metadata by ID
    /// </summary>
    [HttpGet("{mediaId}/info")]
    public async Task<ActionResult<Domain.Models.MediaAsset>> GetMediaById(string mediaId)
    {
        try
        {
            var media = await _mediaService.GetMediaByIdAsync(mediaId);
            if (media == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Media not found: {mediaId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media: {MediaId}", mediaId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting media",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Serves the actual media file content by ID
    /// </summary>
    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetMediaFile(string mediaId)
    {
        try
        {
            var result = await _mediaService.GetMediaFileAsync(mediaId);
            if (result == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Media file not found: {mediaId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var (stream, contentType, fileName) = result.Value;
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving media file: {MediaId}", mediaId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while serving media file",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
