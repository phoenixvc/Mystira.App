using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

