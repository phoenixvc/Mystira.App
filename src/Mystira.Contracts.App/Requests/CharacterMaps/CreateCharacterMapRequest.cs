using System.ComponentModel.DataAnnotations;
using Mystira.App.Domain.Models;

namespace Mystira.Contracts.App.Requests.CharacterMaps;

public class CreateCharacterMapRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Image { get; set; } = string.Empty;

    public string? Audio { get; set; }

    [Required]
    public CharacterMetadata Metadata { get; set; } = new();
}

