using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Badges;

public class CreateBadgeConfigurationRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Axis { get; set; } = string.Empty;

    [Required]
    [Range(0.1, 100.0)]
    public float Threshold { get; set; } = 0.0f;

    [Required]
    public string ImageId { get; set; } = string.Empty;
}

