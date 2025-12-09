using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;
using ErrorResponse = Mystira.App.Contracts.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CharacterMediaMetadataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CharacterMediaMetadataController> _logger;

    public CharacterMediaMetadataController(IMediator mediator, ILogger<CharacterMediaMetadataController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character media metadata file
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CharacterMediaMetadataFile>> GetCharacterMediaMetadataFile()
    {
        try
        {
            var query = new GetCharacterMediaMetadataFileQuery();
            var metadataFile = await _mediator.Send(query);
            return Ok(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character media metadata file");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting character media metadata file",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets a specific character media metadata entry
    /// </summary>
    [HttpGet("entries/{entryId}")]
    public async Task<ActionResult<CharacterMediaMetadataEntry>> GetCharacterMediaMetadataEntry(string entryId)
    {
        try
        {
            var query = new GetCharacterMediaMetadataEntryQuery(entryId);
            var entry = await _mediator.Send(query);

            if (entry == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Character media metadata entry not found: {entryId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting character media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
