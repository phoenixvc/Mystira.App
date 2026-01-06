using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.CharacterMaps.Queries;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for character map management.
/// Follows hexagonal architecture - uses only IMediator (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CharacterMapsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CharacterMapsController> _logger;

    public CharacterMapsController(IMediator mediator, ILogger<CharacterMapsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all character maps
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CharacterMap>>> GetAllCharacterMaps()
    {
        try
        {
            var query = new GetAllCharacterMapsQuery();
            var characterMaps = await _mediator.Send(query);
            return Ok(characterMaps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all character maps");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching character maps",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get character map by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CharacterMap>> GetCharacterMap(string id)
    {
        try
        {
            var query = new GetCharacterMapQuery(id);
            var characterMap = await _mediator.Send(query);
            if (characterMap == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Character map not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(characterMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character map {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
