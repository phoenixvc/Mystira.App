using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Auth;

public class PasswordlessSigninRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

