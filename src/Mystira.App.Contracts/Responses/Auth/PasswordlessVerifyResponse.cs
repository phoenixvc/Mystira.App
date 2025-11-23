using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Responses.Auth;

public class PasswordlessVerifyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Account? Account { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}

