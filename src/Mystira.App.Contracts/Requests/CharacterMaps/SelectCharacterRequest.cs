using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.CharacterMaps;

public class SelectCharacterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CharacterId { get; set; } = string.Empty;
}

