using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Accounts;

public class CreateAccountRequest
{
    [Required]
    [StringLength(200)]
    public string Auth0UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

