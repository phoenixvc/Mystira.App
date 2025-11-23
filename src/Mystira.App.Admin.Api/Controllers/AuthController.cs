using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;

namespace Mystira.App.Admin.Api.Controllers;

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
        // In a real app, validate against a database
        var adminUsername = _configuration["AdminAuth:Username"] ?? "admin";
        var adminPassword = _configuration["AdminAuth:Password"] ?? "adminPass123!";

        // Allow guest access for mobile app
        bool isGuest = request.Username == "guest" && request.Password == "guest";
        bool isAdmin = request.Username == adminUsername && request.Password == adminPassword;

        if (isAdmin || isGuest)
        {
            // Create claims for the authenticated user
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "Guest")
                };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in with cookie authentication
            await HttpContext.SignInAsync(
                "Cookies", // Must match scheme name in AddCookie
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                });

            return Ok(new { Message = "Login successful" });
        }

        return Unauthorized(new { Message = "Invalid username or password" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return Ok(new { Message = "Logged out successfully" });
    }
}
