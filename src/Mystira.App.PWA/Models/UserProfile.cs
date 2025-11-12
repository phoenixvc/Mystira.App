using System.Text.Json.Serialization;

namespace Mystira.App.PWA.Models;

/// <summary>
/// User profile model for PWA
/// </summary>
public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> PreferredFantasyThemes { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public bool IsGuest { get; set; } = false;
    public bool IsNpc { get; set; } = false;
    public string AgeGroupName { get; set; } = "school";
    public int? CurrentAge { get; set; }
    public bool HasCompletedOnboarding { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? AccountId { get; set; }
    
    // Convenience properties
    public string AgeRange => AgeGroupName switch
    {
        "toddlers" => "1-2",
        "preschoolers" => "3-5", 
        "school" => "6-9",
        "preteens" => "10-12",
        "teens" => "13-18",
        _ => "6-9"
    };
    
    public string DisplayAgeRange => AgeRanges.GetDisplayName(AgeRange);
}

/// <summary>
/// Request model for creating a user profile
/// </summary>
public class CreateUserProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> PreferredFantasyThemes { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public bool IsGuest { get; set; } = false;
    public bool IsNpc { get; set; } = false;
    public string AgeGroupName { get; set; } = "school";
    public bool HasCompletedOnboarding { get; set; } = false;
    public string? AccountId { get; set; }
}

/// <summary>
/// Request model for updating a user profile
/// </summary>
public class UpdateUserProfileRequest
{
    public string? Name { get; set; }
    public List<string>? PreferredFantasyThemes { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool? IsGuest { get; set; }
    public bool? IsNpc { get; set; }
    public string? AgeGroupName { get; set; }
    public bool? HasCompletedOnboarding { get; set; }
    public string? AccountId { get; set; }
}

/// <summary>
/// Request model for creating multiple profiles
/// </summary>
public class CreateMultipleProfilesRequest
{
    public List<CreateUserProfileRequest> Profiles { get; set; } = new();
    public string? AccountId { get; set; }
}

/// <summary>
/// Request model for profile assignment
/// </summary>
public class ProfileAssignmentRequest
{
    public string ProfileId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public bool IsNpcAssignment { get; set; } = false;
}
