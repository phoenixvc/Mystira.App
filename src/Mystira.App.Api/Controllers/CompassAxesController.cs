using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompassAxesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompassAxesController> _logger;

    public CompassAxesController(IMediator mediator, ILogger<CompassAxesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompassAxis>>> GetAllCompassAxes()
    {
        _logger.LogInformation("GET: Retrieving all compass axes");
        var axes = await _mediator.Send(new GetAllCompassAxesQuery());
        return Ok(axes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompassAxis>> GetCompassAxisById(string id)
    {
        _logger.LogInformation("GET: Retrieving compass axis with id: {Id}", id);
        var axis = await _mediator.Send(new GetCompassAxisByIdQuery(id));
        if (axis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", id);
            return NotFound(new { message = "Compass axis not found" });
        }
        return Ok(axis);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateCompassAxis([FromBody] ValidateCompassAxisRequest request)
    {
        _logger.LogInformation("POST: Validating compass axis: {Name}", request.Name);

        var isValid = await _mediator.Send(new ValidateCompassAxisQuery(request.Name));
        return Ok(new ValidationResult { IsValid = isValid });
    }
}

public class ValidateCompassAxisRequest
{
    public string Name { get; set; } = string.Empty;
}
