using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Shared.Models;

public class CreateUserProfileRequest
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public List<string> PreferredFantasyThemes { get; set; } = new();
    [Required]
    public string AgeGroup { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public bool IsGuest { get; set; } = false;
    public bool IsNpc { get; set; } = false;
    public string? AccountId { get; set; }
    public bool HasCompletedOnboarding { get; set; }
    public string? SelectedAvatarMediaId { get; set; }
}

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

public class CreateGuestProfileRequest
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Name { get; set; }
    [Required]
    public string AgeGroup { get; set; } = string.Empty;
    public bool UseAdjectiveNames { get; set; } = false;
}

public class CreateMultipleProfilesRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(10)]
    public List<CreateUserProfileRequest> Profiles { get; set; } = new();
}
