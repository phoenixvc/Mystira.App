using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BundlesController : ControllerBase
{
    private readonly IContentBundleService _bundleService;
    private readonly ILogger<BundlesController> _logger;

    public BundlesController(IContentBundleService bundleService, ILogger<BundlesController> logger)
    {
        _bundleService = bundleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContentBundle>>> GetBundles()
    {
        try
        {
            var bundles = await _bundleService.GetAllAsync();
            return Ok(bundles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content bundles");
            return StatusCode(500, new { Message = "Internal server error while fetching bundles", TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpGet("age-group/{ageGroup}")]
    public async Task<ActionResult<List<ContentBundle>>> GetBundlesByAgeGroup(string ageGroup)
    {
        try
        {
            var bundles = await _bundleService.GetByAgeGroupAsync(ageGroup);
            return Ok(bundles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content bundles for age group {AgeGroup}", ageGroup);
            return StatusCode(500, new { Message = "Internal server error while fetching bundles by age group", TraceId = HttpContext.TraceIdentifier });
        }
    }
}
