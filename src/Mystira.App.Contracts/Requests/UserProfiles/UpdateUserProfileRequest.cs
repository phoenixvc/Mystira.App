namespace Mystira.App.Contracts.Requests.UserProfiles;

public class UpdateUserProfileRequest
{
    public List<string>? PreferredFantasyThemes { get; set; }
    public string? AgeGroup { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool? HasCompletedOnboarding { get; set; }
    public bool? IsGuest { get; set; }
    public bool? IsNpc { get; set; }
    public string? AccountId { get; set; }
    public string? Pronouns { get; set; }
    public string? Bio { get; set; }
    public string? SelectedAvatarMediaId { get; set; }
}

