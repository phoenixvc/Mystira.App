using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize] // Admin only
public class MediaMetadataAdminController : ControllerBase
{
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaMetadataAdminController> _logger;

    public MediaMetadataAdminController(IMediaMetadataService mediaMetadataService, ILogger<MediaMetadataAdminController> logger)
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

    /// <summary>
    /// Updates the media metadata file
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<MediaMetadataFile>> UpdateMediaMetadataFile([FromBody] MediaMetadataFile metadataFile)
    {
        try
        {
            var updatedFile = await _mediaMetadataService.UpdateMediaMetadataFileAsync(metadataFile);
            return Ok(updatedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media metadata file");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while updating media metadata file",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets a specific media metadata entry
    /// </summary>
    [HttpGet("entries/{entryId}")]
    public async Task<ActionResult<MediaMetadataEntry>> GetMediaMetadataEntry(string entryId)
    {
        try
        {
            var entry = await _mediaMetadataService.GetMediaMetadataEntryAsync(entryId);
            if (entry == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Media metadata entry not found: {entryId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Adds a new media metadata entry
    /// </summary>
    [HttpPost("entries")]
    public async Task<ActionResult<MediaMetadataFile>> AddMediaMetadataEntry([FromBody] MediaMetadataEntry entry)
    {
        try
        {
            var updatedFile = await _mediaMetadataService.AddMediaMetadataEntryAsync(entry);
            return Ok(updatedFile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding media metadata entry: {EntryId}", entry.Id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while adding media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates an existing media metadata entry
    /// </summary>
    [HttpPut("entries/{entryId}")]
    public async Task<ActionResult<MediaMetadataFile>> UpdateMediaMetadataEntry(string entryId, [FromBody] MediaMetadataEntry entry)
    {
        try
        {
            var updatedFile = await _mediaMetadataService.UpdateMediaMetadataEntryAsync(entryId, entry);
            return Ok(updatedFile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Media metadata entry not found: {entryId}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while updating media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Removes a media metadata entry
    /// </summary>
    [HttpDelete("entries/{entryId}")]
    public async Task<ActionResult<MediaMetadataFile>> RemoveMediaMetadataEntry(string entryId)
    {
        try
        {
            var updatedFile = await _mediaMetadataService.RemoveMediaMetadataEntryAsync(entryId);
            return Ok(updatedFile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Media metadata entry not found: {entryId}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while removing media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Import media metadata entries from JSON
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<MediaMetadataFile>> ImportMediaMetadataEntries([FromBody] string jsonData, [FromQuery] bool overwriteExisting = false)
    {
        try
        {
            var updatedFile = await _mediaMetadataService.ImportMediaMetadataEntriesAsync(jsonData, overwriteExisting);
            return Ok(updatedFile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing media metadata entries");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while importing media metadata entries",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
