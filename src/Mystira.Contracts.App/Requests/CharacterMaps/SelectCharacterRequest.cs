using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.CharacterMaps;

public class SelectCharacterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CharacterId { get; set; } = string.Empty;
}

