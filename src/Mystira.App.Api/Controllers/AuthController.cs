using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("passwordless/signin")]
        public async Task<IActionResult> PasswordlessSignin([FromBody] PasswordlessSigninRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PasswordlessSigninResponse 
                { 
                    Success = false, 
                    Message = "Invalid email address" 
                });
            }

            var (success, message, code) = await _passwordlessAuthService.RequestSigninAsync(request.Email);
            
            _logger.LogInformation("Passwordless signin request: email={Email}, success={Success}", request.Email, success);
            
            return Ok(new PasswordlessSigninResponse 
            { 
                Success = success, 
                Message = message,
                Email = success ? request.Email : null
            });
        }

        [HttpPost("passwordless/signin/verify")]
        public async Task<IActionResult> PasswordlessSigninVerify([FromBody] PasswordlessVerifyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PasswordlessVerifyResponse 
                { 
                    Success = false, 
                    Message = "Invalid email or code" 
                });
            }

            var (success, message, account) = await _passwordlessAuthService.VerifySigninAsync(request.Email, request.Code);
            
            if (!success || account == null)
            {
                return Ok(new PasswordlessVerifyResponse 
                { 
                    Success = false, 
                    Message = message 
                });
            }

            _logger.LogInformation("Passwordless signin verified: email={Email}", request.Email);

            return Ok(new PasswordlessVerifyResponse 
            { 
                Success = true, 
                Message = "Sign-in successful",
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