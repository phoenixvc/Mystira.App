using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.ContentBundles.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BundlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BundlesController> _logger;

    public BundlesController(IMediator mediator, ILogger<BundlesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all content bundles
    /// </summary>
    /// <returns>List of content bundles</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentBundle>>> GetBundles()
    {
        try
        {
            var query = new GetAllContentBundlesQuery();
            var bundles = await _mediator.Send(query);
            return Ok(bundles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content bundles");
            return StatusCode(500, new { Message = "Internal server error while fetching bundles", TraceId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Get content bundles by age group
    /// </summary>
    /// <param name="ageGroup">Age group (e.g., "Ages7to9")</param>
    /// <returns>List of content bundles for the specified age group</returns>
    [HttpGet("age-group/{ageGroup}")]
    public async Task<ActionResult<IEnumerable<ContentBundle>>> GetBundlesByAgeGroup(string ageGroup)
    {
        try
        {
            var query = new GetContentBundlesByAgeGroupQuery(ageGroup);
            var bundles = await _mediator.Send(query);
            return Ok(bundles);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid age group parameter: {AgeGroup}", ageGroup);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content bundles for age group {AgeGroup}", ageGroup);
            return StatusCode(500, new { Message = "Internal server error while fetching bundles by age group", TraceId = HttpContext.TraceIdentifier });
        }
    }
}
