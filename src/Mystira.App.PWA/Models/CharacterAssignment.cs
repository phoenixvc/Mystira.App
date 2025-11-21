using System.Text.Json.Serialization;

namespace Mystira.App.PWA.Models;

/// <summary>
/// Represents a character assignment for a game session
/// </summary>
public class CharacterAssignment
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public PlayerAssignment? PlayerAssignment { get; set; }
    public bool IsUnused { get; set; } = false;

    public string DisplayInfo
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Role))
            {
                parts.Add(Role);
            }

            if (!string.IsNullOrEmpty(Archetype))
            {
                parts.Add(ToTitleCase(Archetype));
            }

            return parts.Count > 0 ? string.Join(" • ", parts) : "Character";
        }
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }
}

/// <summary>
/// Represents a player assigned to a character
/// </summary>
public class PlayerAssignment
{
    public string Type { get; set; } = string.Empty; // "Profile" or "Guest"
    public string? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileImage { get; set; }

    // Guest properties
    public string? GuestName { get; set; }
    public string? GuestAgeRange { get; set; }
    public string? GuestAvatar { get; set; }
    public bool SaveAsProfile { get; set; } = false;
}

/// <summary>
/// Request model for starting a game session with character assignments
/// </summary>
public class StartGameSessionRequest
{
    public string ScenarioId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty; // Primary profile (DM/facilitator)
    public List<CharacterAssignment> CharacterAssignments { get; set; } = new();
    public string TargetAgeGroup { get; set; } = string.Empty;

    [JsonIgnore]
    public Scenario? Scenario { get; set; } // Full scenario for local game session setup
}

/// <summary>
/// Guest profile creation request
/// </summary>
public class CreateGuestProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? AgeRange { get; set; }
    public string? Avatar { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public bool IsGuest { get; set; } = true;
}

/// <summary>
/// Response model for character assignment data
/// </summary>
public class CharacterAssignmentResponse
{
    public Scenario Scenario { get; set; } = new();
    public List<CharacterAssignment> CharacterAssignments { get; set; } = new();
    public List<UserProfile> AvailableProfiles { get; set; } = new();
}

/// <summary>
/// Age range options for guest profiles
/// </summary>
public static class AgeRanges
{
    public static readonly string[] All = ["1-2", "3-5", "6-9", "10-12", "13-18"];

    public static string GetDisplayName(string ageRange)
    {
        return ageRange switch
        {
            "1-2" => "Ages 1-2 (Toddlers)",
            "3-5" => "Ages 3-5 (Preschoolers)",
            "6-9" => "Ages 6-9 (School Age)",
            "10-12" => "Ages 10-12 (Preteens)",
            "13-18" => "Ages 13-18 (Teens)",
            _ => ageRange
        };
    }
}

/// <summary>
/// Pronoun options for guest profiles
/// </summary>
public static class PronounOptions
{
    public static readonly string[] All = ["he/him", "she/her", "they/them", "he/they", "she/they", "prefer not to say"];
}
