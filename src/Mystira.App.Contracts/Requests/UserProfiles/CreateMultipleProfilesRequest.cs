using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.UserProfiles;

public class CreateMultipleProfilesRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(10)] // Reasonable limit for onboarding
    public List<CreateUserProfileRequest> Profiles { get; set; } = new();
}

