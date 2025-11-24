using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;
using Mystira.App.Application.CQRS.MediaAssets.Queries;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;
using ErrorResponse = Mystira.App.Contracts.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMediaApiService _mediaService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediator mediator, IMediaApiService mediaService, ILogger<MediaController> logger)
    {
        _mediator = mediator;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a specific media asset metadata by ID
    /// </summary>
    [HttpGet("{mediaId}/info")]
    [AllowAnonymous]
    public async Task<ActionResult<Domain.Models.MediaAsset>> GetMediaById(string mediaId)
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
