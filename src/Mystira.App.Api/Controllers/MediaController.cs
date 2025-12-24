using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.MediaAssets.Queries;
using Mystira.App.Domain.Models;
using ErrorResponse = Mystira.Contracts.App.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediator mediator, ILogger<MediaController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a specific media asset metadata by ID
    /// </summary>
    [HttpGet("{mediaId}/info")]
    [AllowAnonymous]
    public async Task<ActionResult<MediaAsset>> GetMediaById(string mediaId)
    {
        try
        {
            var query = new GetMediaAssetQuery(mediaId);
            var media = await _mediator.Send(query);
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
    [AllowAnonymous]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetMediaFile(string mediaId)
    {
        try
        {
            var query = new GetMediaFileQuery(mediaId);
            var result = await _mediator.Send(query);

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
