using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.Auth;

public class PasswordlessSigninRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

