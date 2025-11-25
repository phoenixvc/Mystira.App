using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mystira.App.Application.CQRS.Auth.Commands;
using Mystira.App.Contracts.Requests.Auth;
using Mystira.App.Contracts.Responses.Auth;
using Mystira.App.Shared.Logging;

namespace Mystira.App.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [EnableRateLimiting("auth")] // Apply strict rate limiting to all auth endpoints (BUG-5)
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IMediator mediator, ILogger<AuthController> logger)
        {
            _mediator = mediator;
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

            var command = new RequestPasswordlessSignupCommand(request.Email, request.DisplayName);
            var (success, message, code) = await _mediator.Send(command);

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signup request: user={UserHash}, domain={EmailDomain}, success={Success}",
                PiiRedactor.HashEmail(request.Email),
                PiiRedactor.RedactEmail(request.Email),
                success);

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

            var command = new VerifyPasswordlessSignupCommand(request.Email, request.Code);
            var (success, message, account, accessToken, refreshToken) = await _mediator.Send(command);

            if (!success || account == null)
            {
                return Ok(new PasswordlessVerifyResponse
                {
                    Success = false,
                    Message = message
                });
            }

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signup verified: user={UserHash}",
                PiiRedactor.HashEmail(request.Email));

            return Ok(new PasswordlessVerifyResponse
            {
                Success = true,
                Message = message,
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

            var command = new RequestPasswordlessSigninCommand(request.Email);
            var (success, message, code) = await _mediator.Send(command);

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signin request: user={UserHash}, domain={EmailDomain}, success={Success}",
                PiiRedactor.HashEmail(request.Email),
                PiiRedactor.RedactEmail(request.Email),
                success);

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

            var command = new VerifyPasswordlessSigninCommand(request.Email, request.Code);
            var (success, message, account, accessToken, refreshToken) = await _mediator.Send(command);

            if (!success || account == null)
            {
                return Ok(new PasswordlessVerifyResponse
                {
                    Success = false,
                    Message = message
                });
            }

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signin verified: user={UserHash}",
                PiiRedactor.HashEmail(request.Email));

            return Ok(new PasswordlessVerifyResponse
            {
                Success = true,
                Message = message,
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new RefreshTokenResponse
                {
                    Success = false,
                    Message = "Invalid token data"
                });
            }

            var command = new RefreshTokenCommand(request.Token, request.RefreshToken);
            var (success, message, newAccessToken, newRefreshToken) = await _mediator.Send(command);

            if (!success)
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(new RefreshTokenResponse
            {
                Success = true,
                Message = message,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddHours(6),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30)
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
