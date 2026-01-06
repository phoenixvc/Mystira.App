using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Mystira.App.Admin.Web.Controllers;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var adminUsername = _configuration["AdminAuth:Username"] ?? "admin";
        var adminPassword = _configuration["AdminAuth:Password"] ?? "adminPass123!";

        var isGuest = request.Username == "guest" && request.Password == "guest";
        var isAdmin = request.Username == adminUsername && request.Password == adminPassword;

        if (!isAdmin && !isGuest)
        {
            return Unauthorized(new { Message = "Invalid username or password" });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.Role, isAdmin ? "Admin" : "Guest")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            "Cookies",
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            });

        _logger.LogInformation("Admin login successful for {Username} (Role: {Role})", request.Username, isAdmin ? "Admin" : "Guest");

        return Ok(new { Message = "Login successful" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return Ok(new { Message = "Logged out successfully" });
    }
}
