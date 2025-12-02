using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.FantasyThemes.Commands;
using Mystira.App.Application.CQRS.FantasyThemes.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
public class FantasyThemesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FantasyThemesController> _logger;

    public FantasyThemesController(IMediator mediator, ILogger<FantasyThemesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<FantasyThemeDefinition>>> GetAllFantasyThemes()
    {
        _logger.LogInformation("GET: Retrieving all fantasy themes");
        var fantasyThemes = await _mediator.Send(new GetAllFantasyThemesQuery());
        return Ok(fantasyThemes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FantasyThemeDefinition>> GetFantasyThemeById(string id)
    {
        _logger.LogInformation("GET: Retrieving fantasy theme with id: {Id}", id);
        var fantasyTheme = await _mediator.Send(new GetFantasyThemeByIdQuery(id));
        if (fantasyTheme == null)
        {
            _logger.LogWarning("Fantasy theme with id {Id} not found", id);
            return NotFound(new { message = "Fantasy theme not found" });
        }
        return Ok(fantasyTheme);
    }

    [HttpPost]
    public async Task<ActionResult<FantasyThemeDefinition>> CreateFantasyTheme([FromBody] CreateFantasyThemeRequest request)
    {
        _logger.LogInformation("POST: Creating fantasy theme with name: {Name}", request.Name);
        
        var created = await _mediator.Send(new CreateFantasyThemeCommand(request.Name, request.Description));
        return CreatedAtAction(nameof(GetFantasyThemeById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FantasyThemeDefinition>> UpdateFantasyTheme(string id, [FromBody] UpdateFantasyThemeRequest request)
    {
        _logger.LogInformation("PUT: Updating fantasy theme with id: {Id}", id);
        
        var updated = await _mediator.Send(new UpdateFantasyThemeCommand(id, request.Name, request.Description));
        if (updated == null)
        {
            _logger.LogWarning("Fantasy theme with id {Id} not found", id);
            return NotFound(new { message = "Fantasy theme not found" });
        }
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFantasyTheme(string id)
    {
        _logger.LogInformation("DELETE: Deleting fantasy theme with id: {Id}", id);
        
        var success = await _mediator.Send(new DeleteFantasyThemeCommand(id));
        if (!success)
        {
            _logger.LogWarning("Fantasy theme with id {Id} not found", id);
            return NotFound(new { message = "Fantasy theme not found" });
        }
        return NoContent();
    }
}

public class CreateFantasyThemeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateFantasyThemeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
