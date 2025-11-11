using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IPasswordlessAuthService _passwordlessAuthService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, IPasswordlessAuthService passwordlessAuthService, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _passwordlessAuthService = passwordlessAuthService;
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

        [HttpPost("passwordless/signup")]
        public async Task<IActionResult> PasswordlessSignup([FromBody] PasswordlessSignupRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PasswordlessSignupResponse 
                { 
                    Success = false, 
                    Message = "Invalid email or display name" 
                });
            }

            var (success, message, code) = await _passwordlessAuthService.RequestSignupAsync(request.Email, request.DisplayName);
            
            _logger.LogInformation("Passwordless signup request: email={Email}, success={Success}", request.Email, success);
            
            return Ok(new PasswordlessSignupResponse 
            { 
                Success = success, 
                Message = message,
                Email = success ? request.Email : null
            });
        }

        [HttpPost("passwordless/verify")]
        public async Task<IActionResult> PasswordlessVerify([FromBody] PasswordlessVerifyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PasswordlessVerifyResponse 
                { 
                    Success = false, 
                    Message = "Invalid email or code" 
                });
            }

            var (success, message, account) = await _passwordlessAuthService.VerifySignupAsync(request.Email, request.Code);
            
            if (!success || account == null)
            {
                return Ok(new PasswordlessVerifyResponse 
                { 
                    Success = false, 
                    Message = message 
                });
            }

            _logger.LogInformation("Passwordless signup verified: email={Email}", request.Email);

            return Ok(new PasswordlessVerifyResponse 
            { 
                Success = true, 
                Message = "Account created successfully",
                Account = account,
                Token = GenerateDemoToken(account.Auth0UserId)
            });
        }

        private string GenerateDemoToken(string userId)
        {
            return $"demo_token_{userId}_{Guid.NewGuid():N}";
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}