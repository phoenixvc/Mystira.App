using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;
using Mystira.App.Application.CQRS.MediaMetadata.Queries;
using Mystira.App.Contracts.Responses.Common;
using ErrorResponse = Mystira.App.Contracts.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaMetadataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediaMetadataController> _logger;

    public MediaMetadataController(IMediator mediator, ILogger<MediaMetadataController> logger)
    {
        _mediator = mediator;
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
            var query = new GetMediaMetadataFileQuery();
            var metadataFile = await _mediator.Send(query);
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
