using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for client status and update information
/// </summary>
[ApiController]
[Route("api/client")]
[Produces("application/json")]
public class ClientController : ControllerBase
{
    private readonly ILogger<ClientController> _logger;
    private readonly IClientApiService _clientService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientController"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="clientService">The client service</param>
    public ClientController(
        ILogger<ClientController> logger,
        IClientApiService clientService)
    {
        _logger = logger;
        _clientService = clientService;
    }

    /// <summary>
    /// Get client status information including version requirements and content updates
    /// </summary>
    /// <param name="clientVersion">Current client version (e.g., 1.3.0)</param>
    /// <param name="contentVersion">Current content bundle version (e.g., 2025-05-28)</param>
    /// <returns>Status information with version requirements and content updates</returns>
    /// <response code="200">Returns the client status information</response>
    /// <response code="400">If the client version format is invalid</response>
    /// <response code="401">If the request is not authorized</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("status")]
    [Authorize] // Requires authentication
    [ProducesResponseType(typeof(ClientStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClientStatusResponse>> GetClientStatus(
        [FromQuery(Name = "client_version")] string clientVersion,
        [FromQuery(Name = "content_version")] string contentVersion)
    {
        try
        {
            _logger.LogInformation("Client status check: client_version={ClientVersion}, content_version={ContentVersion}", 
                clientVersion, contentVersion);

            // Delegate to the service
            var response = await _clientService.GetClientStatusAsync(clientVersion, contentVersion);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid client version format");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client status request");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while processing client status",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
