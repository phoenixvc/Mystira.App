using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Avatars.Queries;
using Mystira.Contracts.App.Responses.Common;
using Mystira.Contracts.App.Responses.Media;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for avatar configuration management.
/// Follows hexagonal architecture - uses only IMediator (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AvatarsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AvatarsController> _logger;

    public AvatarsController(IMediator mediator, ILogger<AvatarsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available avatars grouped by age group
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AvatarResponse>> GetAvatars()
    {
        try
        {
            var query = new GetAvatarsQuery();
            var avatars = await _mediator.Send(query);
            return Ok(avatars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatars");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting avatars",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets avatars for a specific age group
    /// </summary>
    [HttpGet("{ageGroup}")]
    public async Task<ActionResult<AvatarConfigurationResponse>> GetAvatarsByAgeGroup(string ageGroup)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Age group is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var query = new GetAvatarsByAgeGroupQuery(ageGroup);
            var avatars = await _mediator.Send(query);

            if (avatars == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"No avatars found for age group: {ageGroup}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(avatars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatars for age group: {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting avatars",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
