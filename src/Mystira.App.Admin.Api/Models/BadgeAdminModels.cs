using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Mystira.App.Admin.Api.Models;

public class BadgeDto
{
    public string Id { get; set; } = string.Empty;
    public string AgeGroupId { get; set; } = string.Empty;
    public string AgeGroupName { get; set; } = string.Empty;
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public string CompassAxisId { get; set; } = string.Empty;
    public string CompassAxisName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int TierOrder { get; set; }
    public float RequiredScore { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
    public BadgeImageDto? Image { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BadgeImageDto
{
    public string Id { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public long FileSizeBytes { get; set; }
    public string? DataUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AxisAchievementDto
{
    public string Id { get; set; } = string.Empty;
    public string AgeGroupId { get; set; } = string.Empty;
    public string CompassAxisId { get; set; } = string.Empty;
    public string CompassAxisName { get; set; } = string.Empty;
    public string AxesDirection { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BadgeQueryOptions
{
    public string? AgeGroupId { get; set; }
    public string? CompassAxisId { get; set; }
    public string? Tier { get; set; }
    public string? Search { get; set; }
    public bool IncludeAxisMetadata { get; set; } = true;
    public bool IncludeImages { get; set; } = true;
}

public class BadgeSnapshotDto
{
    public string AgeGroupId { get; set; } = string.Empty;
    public string AgeGroupName { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; }
    public List<AxisAchievementDto> AxisAchievements { get; set; } = new();
    public List<BadgeDto> Badges { get; set; } = new();
}

public class BadgeImportResult
{
    public bool Success { get; set; }
    public string? AgeGroupId { get; set; }
    public bool Overwrite { get; set; }
    public int CreatedAxisAchievements { get; set; }
    public int UpdatedAxisAchievements { get; set; }
    public int CreatedBadges { get; set; }
    public int UpdatedBadges { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BadgeImageUploadRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string ImageId { get; set; } = string.Empty;

    [Required]
    public IFormFile? File { get; set; }
}

public class CreateBadgeRequest
{
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string AgeGroupId { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CompassAxisId { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string Tier { get; set; } = string.Empty;

    [Range(1, 100)]
    public int TierOrder { get; set; } = 1;

    [Range(0.01, 1000)]
    public float RequiredScore { get; set; } = 1.0f;

    [Required]
    [StringLength(300, MinimumLength = 2)]
    public string ImageId { get; set; } = string.Empty;
}

public class UpdateBadgeRequest
{
    public string? AgeGroupId { get; set; }
    public string? CompassAxisId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Tier { get; set; }
    public int? TierOrder { get; set; }
    public float? RequiredScore { get; set; }
    public string? ImageId { get; set; }
}

public class AxisAchievementRequest
{
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string AgeGroupId { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CompassAxisId { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string AxesDirection { get; set; } = "positive";

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;
}
