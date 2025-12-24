namespace Mystira.Contracts.App.Responses.Auth;

public class PasswordlessSignupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>
    /// Detailed error information. Only populated in development mode for debugging purposes.
    /// </summary>
    public string? ErrorDetails { get; set; }
}

