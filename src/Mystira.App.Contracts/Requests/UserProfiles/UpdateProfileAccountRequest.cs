using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.UserProfiles;

public class UpdateProfileAccountRequest
{
    /// <summary>
    /// Email address of the account to associate the profile with
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

