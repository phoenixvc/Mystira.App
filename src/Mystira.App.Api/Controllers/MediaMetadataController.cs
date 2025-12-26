using Wolverine;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.MediaMetadata.Queries;
using ErrorResponse = Mystira.Contracts.App.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MediaMetadataController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<MediaMetadataController> _logger;

    public MediaMetadataController(IMessageBus bus, ILogger<MediaMetadataController> logger)
    {
        _bus = bus;
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
            var metadataFile = await _bus.InvokeAsync<MediaMetadataFile>(query);
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
