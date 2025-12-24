using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.Auth;

public class PasswordlessVerifyRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

