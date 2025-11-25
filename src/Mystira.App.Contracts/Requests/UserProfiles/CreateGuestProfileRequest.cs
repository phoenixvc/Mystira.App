using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.UserProfiles;

public class CreateGuestProfileRequest
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Optional name for guest profile. If not provided, a random name will be generated.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Age group for the guest profile
    /// </summary>
    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use adjectives in random name generation
    /// </summary>
    public bool UseAdjectiveNames { get; set; } = false;
}

