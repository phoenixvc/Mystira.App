using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.UserProfiles;

public class CreateUserProfileRequest
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public List<string> PreferredFantasyThemes { get; set; } = new();

    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth for age calculation (optional for guest profiles)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Indicates if this is a guest profile
    /// </summary>
    public bool IsGuest { get; set; } = false;

    /// <summary>
    /// Indicates if this profile represents an NPC
    /// </summary>
    public bool IsNpc { get; set; } = false;

    /// <summary>
    /// Identifier representing the associated account.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Indicates if the user has completed onboarding
    /// </summary>
    public bool HasCompletedOnboarding { get; set; }

    /// <summary>
    /// Media ID for the user's selected avatar
    /// </summary>
    public string? SelectedAvatarMediaId { get; set; }

    /// <summary>
    /// Pronouns for the profile (e.g., they/them, she/her, he/him)
    /// </summary>
    public string? Pronouns { get; set; }

    /// <summary>
    /// Bio or description for the profile
    /// </summary>
    public string? Bio { get; set; }
}

