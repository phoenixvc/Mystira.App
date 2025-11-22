using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Contracts.Responses.Common;
using ErrorResponse = Mystira.App.Contracts.Responses.Common.ErrorResponse;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize]
public class MediaAdminController : ControllerBase
{
    private readonly IMediaApiService _mediaService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaAdminController> _logger;

    public MediaAdminController(IMediaApiService mediaService, IMediaMetadataService mediaMetadataService, ILogger<MediaAdminController> logger)
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
    /// Upload a single media file (Admin authentication required)
    /// Media ID must match an existing entry in the media metadata file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<MediaAsset>> UploadMedia([FromForm] IFormFile file, [FromForm] string? mediaId = null, [FromForm] string? mediaType = null, [FromForm] string? description = null, [FromForm] string? tags = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "No file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Get media metadata file to resolve media ID from filename if needed
            var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
            if (metadataFile == null || metadataFile.Entries.Count == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "No media metadata file found. Please upload a media metadata file first.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Resolve media ID and type from metadata
            MediaMetadataEntry? metadataEntry = null;

            if (!string.IsNullOrEmpty(mediaId))
            {
                // Find by provided media ID
                metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == mediaId);
                if (metadataEntry == null)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = $"No metadata entry found for media ID: {mediaId}",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }
            }
            else
            {
                // Find by filename
                metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == file.FileName);
                if (metadataEntry == null)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = $"No metadata entry found for filename: {file.FileName}. Please specify a valid media ID or ensure the filename matches a metadata entry.",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }
                mediaId = metadataEntry.Id;
            }

            // Use metadata type if not provided
            if (string.IsNullOrEmpty(mediaType))
            {
                mediaType = metadataEntry.Type;
            }

            var tagsList = string.IsNullOrEmpty(tags) ? null : tags.Split(',').Select(t => t.Trim()).ToList();
            var mediaAsset = await _mediaService.UploadMediaAsync(file, mediaId, mediaType, description, tagsList);

            return Ok(mediaAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media file: {FileName}", file?.FileName);
            return StatusCode(500, new ErrorResponse
            {
                Message = ex.Message.Contains("already exists") ? ex.Message : "Internal server error while uploading media",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Upload multiple media files (Admin authentication required)
    /// Filenames must match entries in the existing media metadata file
    /// </summary>
    [HttpPost("bulk-upload")]
    public async Task<ActionResult<BulkUploadResult>> BulkUploadMedia([FromForm] IFormFile[] files, [FromForm] IFormFile? metadataFile = null, [FromForm] bool autoDetectType = true, [FromForm] bool overwriteExisting = false)
    {
        try
        {
            if (files == null || files.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "No files provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Process metadata file if provided
            if (metadataFile != null)
            {
                try
                {
                    using var stream = metadataFile.OpenReadStream();
                    using var reader = new StreamReader(stream);
                    var jsonData = await reader.ReadToEndAsync();

                    await _mediaMetadataService.ImportMediaMetadataEntriesAsync(jsonData, overwriteExisting);
                    _logger.LogInformation("Media metadata imported successfully from file: {FileName}", metadataFile.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing media metadata file: {FileName}", metadataFile.FileName);
                    return BadRequest(new ErrorResponse
                    {
                        Message = $"Error importing metadata file: {ex.Message}",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }
            }

            var result = await _mediaService.BulkUploadMediaAsync(files, autoDetectType, overwriteExisting);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk uploading media files");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while bulk uploading media",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a media asset
    /// </summary>
    [HttpPut("{mediaId}")]
    public async Task<ActionResult<MediaAsset>> UpdateMedia(string mediaId, [FromBody] MediaUpdateRequest updateData)
    {
        try
        {
            var updatedMedia = await _mediaService.UpdateMediaAsync(mediaId, updateData);
            return Ok(updatedMedia);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Media not found: {mediaId}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media: {MediaId}", mediaId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while updating media",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a media asset (Admin authentication required)
    /// </summary>
    [HttpDelete("{mediaId}")]
    public async Task<ActionResult> DeleteMedia(string mediaId)
    {
        try
        {
            var deleted = await _mediaService.DeleteMediaAsync(mediaId);
            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Media not found: {mediaId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media: {MediaId}", mediaId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while deleting media",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Validate media references
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<MediaValidationResult>> ValidateMediaReferences([FromBody] List<string> mediaReferences)
    {
        try
        {
            var result = await _mediaService.ValidateMediaReferencesAsync(mediaReferences);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating media references");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while validating media references",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get media usage statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<MediaUsageStats>> GetMediaStatistics()
    {
        try
        {
            var stats = await _mediaService.GetMediaUsageStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media statistics");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting media statistics",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Upload media from a zip file containing media-metadata.json and media files
    /// Processes metadata first, then uploads media files if metadata import succeeds
    /// </summary>
    [HttpPost("upload-zip")]
    public async Task<ActionResult<ZipUploadResult>> UploadMediaZip(
        [FromForm] IFormFile zipFile,
        [FromForm] bool overwriteMetadata = false,
        [FromForm] bool overwriteMedia = false)
    {
        try
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "No zip file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "File must be a zip file (.zip extension required)",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _mediaService.UploadMediaFromZipAsync(zipFile, overwriteMetadata, overwriteMedia);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media from zip file: {FileName}", zipFile?.FileName);
            return StatusCode(500, new ZipUploadResult
            {
                Success = false,
                Message = "Internal server error while uploading media from zip",
                AllErrors = new List<string> { ex.Message }
            });
        }
    }
}
