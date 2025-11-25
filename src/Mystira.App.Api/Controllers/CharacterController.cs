using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;
using Mystira.App.Application.CQRS.Characters.Queries;
using Mystira.App.Contracts.Requests.CharacterMaps;
using Mystira.App.Contracts.Responses.Common;
using ErrorResponse = Mystira.App.Contracts.Responses.Common.ErrorResponse;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CharacterController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CharacterController> _logger;

    public CharacterController(IMediator mediator, ILogger<CharacterController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a specific character by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Character>> GetCharacter(string id)
    {
        try
        {
            var query = new GetCharacterQuery(id);
            var character = await _mediator.Send(query);

            if (character == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Character not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character: {CharacterId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
