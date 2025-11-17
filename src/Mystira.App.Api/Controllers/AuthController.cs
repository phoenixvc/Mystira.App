using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, IPasswordlessAuthService passwordlessAuthService, IJwtService jwtService, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _passwordlessAuthService = passwordlessAuthService;
            _jwtService = jwtService;
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

            var accessToken = _jwtService.GenerateAccessToken(account.Auth0UserId, account.Email, account.DisplayName);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new PasswordlessVerifyResponse 
            { 
                Success = true, 
                Message = "Account created successfully",
                Account = account,
                Token = accessToken,
                RefreshToken = refreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddHours(6),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30) // Refresh token valid for 30 days
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

            var accessToken = _jwtService.GenerateAccessToken(account.Auth0UserId, account.Email, account.DisplayName);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new PasswordlessVerifyResponse 
            { 
                Success = true, 
                Message = "Sign-in successful",
                Account = account,
                Token = accessToken,
                RefreshToken = refreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddHours(6),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30) // Refresh token valid for 30 days
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new RefreshTokenResponse 
                    { 
                        Success = false, 
                        Message = "Invalid token data" 
                    });
                }

                // Validate the current access token to get user info
                var (isValid, userId) = _jwtService.ValidateAndExtractUserId(request.Token);
                
                if (!isValid || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new RefreshTokenResponse 
                    { 
                        Success = false, 
                        Message = "Invalid access token" 
                    });
                }

                // In a real implementation, you would validate the refresh token against stored data
                // For now, we'll just generate new tokens (this is a simplified approach)
                // In production, you should store refresh tokens in a database and validate them
                
                // Get user account info
                var account = await _passwordlessAuthService.GetAccountByUserIdAsync(userId);
                if (account == null)
                {
                    return Unauthorized(new RefreshTokenResponse 
                    { 
                        Success = false, 
                        Message = "User not found" 
                    });
                }

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateAccessToken(account.Auth0UserId, account.Email, account.DisplayName);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

                return Ok(new RefreshTokenResponse 
                { 
                    Success = true, 
                    Message = "Token refreshed successfully",
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiresAt = DateTime.UtcNow.AddHours(6),
                    RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new RefreshTokenResponse 
                { 
                    Success = false, 
                    Message = "An error occurred while refreshing token" 
                });
            }
        }

        private string GenerateDemoToken(string accountId, string email, string displayName)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "Mystira-app-Development-Secret-Key-2024-Very-Long-For-Security";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "mystira-app-api";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "mystira-app";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, accountId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name, displayName),
                new Claim("account_id", accountId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}