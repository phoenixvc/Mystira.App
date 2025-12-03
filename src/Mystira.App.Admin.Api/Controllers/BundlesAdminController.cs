using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize] // Requires authentication for admin operations
public class BundlesAdminController : ControllerBase
{
    private readonly IContentBundleAdminService _service;
    private readonly ILogger<BundlesAdminController> _logger;

    public BundlesAdminController(IContentBundleAdminService service, ILogger<BundlesAdminController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContentBundle>>> GetAll()
    {
        try
        {
            var bundles = await _service.GetAllAsync();
            return Ok(bundles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundles");
            return StatusCode(500, new { Message = "Internal server error while fetching bundles", TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContentBundle>> GetById(string id)
    {
        try
        {
            var bundle = await _service.GetByIdAsync(id);
            if (bundle == null)
            {
                return NotFound(new { Message = $"Bundle not found: {id}", TraceId = HttpContext.TraceIdentifier });
            }

            return Ok(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundle {BundleId}", id);
            return StatusCode(500, new { Message = "Internal server error while fetching bundle", TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ContentBundle>> Create([FromBody] ContentBundle bundle)
    {
        try
        {
            var created = await _service.CreateAsync(bundle);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bundle");
            return StatusCode(500, new { Message = "Internal server error while creating bundle", TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContentBundle>> Update(string id, [FromBody] ContentBundle bundle)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, bundle);
            if (updated == null)
            {
                return NotFound(new { Message = $"Bundle not found: {id}", TraceId = HttpContext.TraceIdentifier });
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle {BundleId}", id);
            return StatusCode(500, new { Message = "Internal server error while updating bundle", TraceId = HttpContext.TraceIdentifier });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { Message = $"Bundle not found: {id}", TraceId = HttpContext.TraceIdentifier });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bundle {BundleId}", id);
            return StatusCode(500, new { Message = "Internal server error while deleting bundle", TraceId = HttpContext.TraceIdentifier });
        }
    }
}
