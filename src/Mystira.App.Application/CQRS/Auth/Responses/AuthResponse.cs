using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Responses;

/// <summary>
/// Response for a passwordless signin request.
/// </summary>
public record AuthResponse(
    bool Success,
    string Message,
    string? Code = null,
    string? ErrorDetails = null,
    Account? Account = null,
    string? AccessToken = null,
    string? RefreshToken = null
);
