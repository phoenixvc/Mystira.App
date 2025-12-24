using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.Auth;

public class PasswordlessSignupRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

