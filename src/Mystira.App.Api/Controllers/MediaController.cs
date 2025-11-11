using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

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
    /// Gets all media assets with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MediaQueryResponse>> GetMedia([FromQuery] MediaQueryRequest request)
    {
        try
        {
            var result = await _mediaService.GetMediaAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media assets");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting media",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get total count of media assets
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<object>> GetMediaCount()
    {
        try
        {
            var request = new MediaQueryRequest { PageSize = 1 };
            var result = await _mediaService.GetMediaAsync(request);
            return Ok(new { count = result.TotalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media count");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting media count",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets a specific media asset metadata by ID
    /// </summary>
    [HttpGet("{mediaId}/info")]
    public async Task<ActionResult<MediaAsset>> GetMediaById(string mediaId)
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

    /// <summary>
    /// Gets a media asset by filename, resolving through metadata
    /// </summary>
    [HttpGet("by-filename/{fileName}")]
    public async Task<ActionResult<MediaAsset>> GetMediaByFileName(string fileName)
    {
        try
        {
            var media = await _mediaService.GetMediaByFileNameAsync(fileName);
            if (media == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Media not found for filename: {fileName}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media by filename: {FileName}", fileName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting media by filename",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets the URL for a media asset by filename, resolving through metadata
    /// </summary>
    [HttpGet("url/{fileName}")]
    public async Task<ActionResult<string>> GetMediaUrl(string fileName)
    {
        try
        {
            var url = await _mediaService.GetMediaUrlAsync(fileName);
            if (url == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Media URL not found for filename: {fileName}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media URL by filename: {FileName}", fileName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting media URL by filename",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Serves media file content by filename, resolving through metadata
    /// </summary>
    [HttpGet("file/{fileName}")]
    public async Task<IActionResult> GetMediaFileByFileName(string fileName)
    {
        try
        {
            var mediaAsset = await _mediaService.GetMediaByFileNameAsync(fileName);
            if (mediaAsset == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Media file not found for filename: {fileName}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _mediaService.GetMediaFileAsync(mediaAsset.MediaId);
            if (result == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Media file content not found: {fileName}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var (stream, contentType, originalFileName) = result.Value;
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving media file by filename: {FileName}", fileName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while serving media file by filename",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
