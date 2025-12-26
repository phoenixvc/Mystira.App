using Microsoft.AspNetCore.Mvc;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Auth controller for Entra External Identities integration.
/// Note: Authentication is handled client-side via MSAL with Entra External Identities.
/// This controller provides status/info endpoints only.
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns authentication configuration info for clients.
    /// </summary>
    [HttpGet("config")]
    public ActionResult GetAuthConfig()
    {
        // Return minimal config info - actual auth is handled by MSAL client-side
        return Ok(new
        {
            provider = "Entra External Identities",
            message = "Authentication is handled client-side via MSAL"
        });
    }
}
