namespace Mystira.Contracts.App.Models.GameSessions;

public class CharacterAssignmentDto
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public PlayerAssignmentDto? PlayerAssignment { get; set; }
    public bool IsUnused { get; set; }
}

public class PlayerAssignmentDto
{
    public string Type { get; set; } = string.Empty; // "Player" or "Guest"
    public string? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileImage { get; set; }
    public string? SelectedAvatarMediaId { get; set; }

    // Guest fields
    public string? GuestName { get; set; }
    public string? GuestAgeRange { get; set; }
    public string? GuestAvatar { get; set; }
    public bool SaveAsProfile { get; set; }
}
