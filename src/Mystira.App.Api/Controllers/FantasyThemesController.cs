using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.FantasyThemes.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("validate")]
    public async Task<ActionResult<dynamic>> ValidateFantasyTheme([FromBody] ValidateFantasyThemeRequest request)
    {
        _logger.LogInformation("POST: Validating fantasy theme: {Name}", request.Name);

        var isValid = await _mediator.Send(new ValidateFantasyThemeQuery(request.Name));
        return Ok(new { isValid });
    }
}

public class ValidateFantasyThemeRequest
{
    public string Name { get; set; } = string.Empty;
}
