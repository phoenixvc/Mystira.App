using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompassAxesController : ControllerBase
{
    private readonly ICompassAxisApiService _compassAxisService;
    private readonly ILogger<CompassAxesController> _logger;

    public CompassAxesController(ICompassAxisApiService compassAxisService, ILogger<CompassAxesController> logger)
    {
        _compassAxisService = compassAxisService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompassAxis>>> GetAllCompassAxes()
    {
        _logger.LogInformation("GET: Retrieving all compass axes");
        var axes = await _compassAxisService.GetAllCompassAxesAsync();
        return Ok(axes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompassAxis>> GetCompassAxisById(string id)
    {
        _logger.LogInformation("GET: Retrieving compass axis with id: {Id}", id);
        var axis = await _compassAxisService.GetCompassAxisByIdAsync(id);
        if (axis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", id);
            return NotFound(new { message = "Compass axis not found" });
        }
        return Ok(axis);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<dynamic>> ValidateCompassAxis([FromBody] ValidateCompassAxisRequest request)
    {
        _logger.LogInformation("POST: Validating compass axis: {Name}", request.Name);
        
        var isValid = await _compassAxisService.IsValidCompassAxisAsync(request.Name);
        return Ok(new { isValid });
    }
}

public class ValidateCompassAxisRequest
{
    public string Name { get; set; } = string.Empty;
}
