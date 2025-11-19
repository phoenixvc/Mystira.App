namespace Mystira.App.PWA.Models;

public class PasswordlessSignupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
}

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

public class PasswordlessSigninResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}
