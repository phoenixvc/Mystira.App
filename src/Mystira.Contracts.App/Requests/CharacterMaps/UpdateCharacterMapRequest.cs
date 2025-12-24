using Mystira.App.Domain.Models;

namespace Mystira.Contracts.App.Requests.CharacterMaps;

public class UpdateCharacterMapRequest
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public CharacterMetadata? Metadata { get; set; }
}

