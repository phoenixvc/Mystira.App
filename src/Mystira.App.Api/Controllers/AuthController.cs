using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mystira.App.Application.CQRS.Auth.Commands;
using Mystira.Contracts.App.Requests.Auth;
using Mystira.Contracts.App.Responses.Auth;
using Mystira.App.Shared.Logging;

namespace Mystira.App.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AuthController(IMediator mediator, ILogger<AuthController> logger, IWebHostEnvironment environment)
        {
            _mediator = mediator;
            _logger = logger;
            _environment = environment;
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
            var response = await _mediator.Send(command);

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signup request: user={UserHash}, domain={EmailDomain}, success={Success}",
                PiiRedactor.HashEmail(request.Email),
                PiiRedactor.RedactEmail(request.Email),
                response.Success);

            return Ok(new PasswordlessSignupResponse
            {
                Success = response.Success,
                Message = response.Message,
                Email = response.Success ? request.Email : null,
                // Only include error details in development mode for debugging
                ErrorDetails = _environment.IsDevelopment() ? response.ErrorDetails : null
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
            var response = await _mediator.Send(command);

            if (!response.Success || response.Account == null)
            {
                return Ok(new PasswordlessVerifyResponse
                {
                    Success = false,
                    Message = response.Message,
                    // Only include error details in development mode for debugging
                    ErrorDetails = _environment.IsDevelopment() ? response.ErrorDetails : null
                });
            }

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signup verified: user={UserHash}",
                PiiRedactor.HashEmail(request.Email));

            return Ok(new PasswordlessVerifyResponse
            {
                Success = true,
                Message = response.Message,
                Account = response.Account,
                Token = response.AccessToken,
                RefreshToken = response.RefreshToken,
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
            var response = await _mediator.Send(command);

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signin request: user={UserHash}, domain={EmailDomain}, success={Success}",
                PiiRedactor.HashEmail(request.Email),
                PiiRedactor.RedactEmail(request.Email),
                response.Success);

            return Ok(new PasswordlessSigninResponse
            {
                Success = response.Success,
                Message = response.Message,
                Email = response.Success ? request.Email : null,
                // Only include error details in development mode for debugging
                ErrorDetails = _environment.IsDevelopment() ? response.ErrorDetails : null
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
            var response = await _mediator.Send(command);

            if (!response.Success || response.Account == null)
            {
                return Ok(new PasswordlessVerifyResponse
                {
                    Success = false,
                    Message = response.Message,
                    // Only include error details in development mode for debugging
                    ErrorDetails = _environment.IsDevelopment() ? response.ErrorDetails : null
                });
            }

            // Use PII redaction for COPPA/GDPR compliance
            _logger.LogInformation("Passwordless signin verified: user={UserHash}",
                PiiRedactor.HashEmail(request.Email));

            return Ok(new PasswordlessVerifyResponse
            {
                Success = true,
                Message = response.Message,
                Account = response.Account,
                Token = response.AccessToken,
                RefreshToken = response.RefreshToken,
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
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    Message = response.Message
                });
            }

            return Ok(new RefreshTokenResponse
            {
                Success = true,
                Message = response.Message,
                Token = response.AccessToken,
                RefreshToken = response.RefreshToken,
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
