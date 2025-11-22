using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.UserProfiles;

public class ProfileAssignmentRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ProfileId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CharacterId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this profile should be treated as an NPC for this assignment
    /// </summary>
    public bool IsNpcAssignment { get; set; } = false;
}

