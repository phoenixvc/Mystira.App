using Mystira.App.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Api.Models;

// Request DTOs
public class CreateUserProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public List<string> PreferredFantasyThemes { get; set; } = new();

    [Required]
    public string AgeGroup { get; set; } = string.Empty;
    
    /// <summary>
    /// Date of birth for age calculation (optional for guest profiles)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// Indicates if this is a guest profile
    /// </summary>
    public bool IsGuest { get; set; } = false;
    
    /// <summary>
    /// Indicates if this profile represents an NPC
    /// </summary>
    public bool IsNpc { get; set; } = false;
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
}

public class CreateGuestProfileRequest
{
    /// <summary>
    /// Optional name for guest profile. If not provided, a random name will be generated.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Age group for the guest profile
    /// </summary>
    [Required]
    public string AgeGroup { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to use adjectives in random name generation
    /// </summary>
    public bool UseAdjectiveNames { get; set; } = false;
}

public class CreateScenarioRequest
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public List<string> Tags { get; set; } = new();

    [Required]
    public DifficultyLevel Difficulty { get; set; }

    [Required]
    public SessionLength SessionLength { get; set; }

    [Required]
    [MaxLength(4)]
    public List<string> Archetypes { get; set; } = new();

    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    [Required]
    public int MinimumAge { get; set; }

    [Required]
    [MaxLength(4)]
    public List<string> CoreAxes { get; set; } = new();

    [Required]
    public List<ScenarioCharacter> Characters { get; set; } = new();

    [Required]
    public List<Scene> Scenes { get; set; } = new();

    public List<string> CompassAxes { get; set; } = new();
}

public class StartGameSessionRequest
{
    [Required]
    public string ScenarioId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DmAccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DmProfileId { get; set; } = string.Empty;

    [Required]
    public List<string> PlayerNames { get; set; } = new();

    [Required]
    public string TargetAgeGroup { get; set; } = string.Empty;
}

public class MakeChoiceRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string SceneId { get; set; } = string.Empty;

    [Required]
    public string ChoiceText { get; set; } = string.Empty;

    [Required]
    public string NextSceneId { get; set; } = string.Empty;
}

public class ScenarioQueryRequest
{
    public DifficultyLevel? Difficulty { get; set; }
    public SessionLength? SessionLength { get; set; }
    public int? MinimumAge { get; set; }
    public string? AgeGroup { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Archetypes { get; set; }
    public List<string>? CoreAxes { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// Response DTOs
public class ScenarioListResponse
{
    public List<ScenarioSummary> Scenarios { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public class ScenarioSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DifficultyLevel Difficulty { get; set; }
    public SessionLength SessionLength { get; set; }
    public List<string> Archetypes { get; set; } = new();
    public int MinimumAge { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public List<string> CoreAxes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class GameSessionResponse
{
    public string Id { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string DmAccountId { get; set; } = string.Empty;
    public string DmProfileId { get; set; } = string.Empty;
    public List<string> PlayerNames { get; set; } = new();
    public SessionStatus Status { get; set; }
    public string CurrentSceneId { get; set; } = string.Empty;
    public int ChoiceCount { get; set; }
    public int EchoCount { get; set; }
    public int AchievementCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public bool IsPaused { get; set; }
    public int SceneCount { get; set; }
    public string TargetAgeGroup { get; set; } = string.Empty;
}

public class SessionStatsResponse
{
    public Dictionary<string, double> CompassValues { get; set; } = new();
    public List<EchoLog> RecentEchoes { get; set; } = new();
    public List<SessionAchievement> Achievements { get; set; } = new();
    public int TotalChoices { get; set; }
    public TimeSpan SessionDuration { get; set; }
}

// Health Check Models
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

// Error Response Models
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}

public class ValidationErrorResponse : ErrorResponse
{
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();
}

// Client Status Models
/// <summary>
/// Response from the client status API endpoint containing version information and content updates
/// </summary>
public class ClientStatusResponse
{
    /// <summary>
    /// Whether the client should force a content refresh regardless of other conditions
    /// </summary>
    public bool ForceRefresh { get; set; }
    
    /// <summary>
    /// The minimum supported client version
    /// </summary>
    public string MinSupportedVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// The latest available client version
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// A user-friendly message about the status
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The content manifest containing changes to scenarios and media
    /// </summary>
    public ContentManifest ContentManifest { get; set; } = new();
    
    /// <summary>
    /// The current content bundle version
    /// </summary>
    public string BundleVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether an update is required (client version below minimum supported)
    /// </summary>
    public bool UpdateRequired { get; set; }
}

/// <summary>
/// Represents a request to retrieve client status information, including client and content version details.
/// </summary>
public class ClientStatusRequest
{
    public string ClientVersion { get; set; } = string.Empty;
    public string ContentVersion { get; set; } = string.Empty;
}

/// <summary>
/// Content manifest containing changes to scenarios and media
/// </summary>
public class ContentManifest
{
    /// <summary>
    /// Changes to scenarios (added, updated, removed)
    /// </summary>
    public ScenarioChanges Scenarios { get; set; } = new();
    
    /// <summary>
    /// Changes to media files (added, updated, removed)
    /// </summary>
    public MediaChanges Media { get; set; } = new();
    
    /// <summary>
    /// The current content bundle version
    /// </summary>
    public string BundleVersion { get; set; } = string.Empty;
}

/// <summary>
/// Changes to scenarios (added, updated, removed)
/// </summary>
public class ScenarioChanges
{
    /// <summary>
    /// List of scenario IDs that have been added
    /// </summary>
    public List<string> Added { get; set; } = new();
    
    /// <summary>
    /// List of scenario IDs that have been updated
    /// </summary>
    public List<string> Updated { get; set; } = new();
    
    /// <summary>
    /// List of scenario IDs that have been removed
    /// </summary>
    public List<string> Removed { get; set; } = new();
}

/// <summary>
/// Changes to media files (added, updated, removed)
/// </summary>
public class MediaChanges
{
    /// <summary>
    /// List of media items that have been added
    /// </summary>
    public List<MediaItem> Added { get; set; } = new();
    
    /// <summary>
    /// List of media items that have been updated
    /// </summary>
    public List<MediaItem> Updated { get; set; } = new();
    
    /// <summary>
    /// List of media IDs that have been removed
    /// </summary>
    public List<string> Removed { get; set; } = new();
}

/// <summary>
/// Information about a media file
/// </summary>
public class MediaItem
{
    /// <summary>
    /// The unique identifier for the media
    /// </summary>
    public string MediaId { get; set; } = string.Empty;
    
    /// <summary>
    /// The file path for the media
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// The version of the media (typically a timestamp)
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// A hash of the media content for integrity verification
    /// </summary>
    public string Hash { get; set; } = string.Empty;
}

// Account API Models
public class CreateAccountRequest
{
    [Required]
    [StringLength(200)]
    public string Auth0UserId { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

public class UpdateAccountRequest
{
    public string? DisplayName { get; set; }
    public AccountSettings? Settings { get; set; }
}

public class UpdateSubscriptionRequest
{
    [Required]
    public SubscriptionType Type { get; set; }
    
    public string? ProductId { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? PurchaseToken { get; set; }
    public List<string>? PurchasedScenarios { get; set; }
}

// Character Map API Models
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
    public App.Domain.Models.CharacterMetadata Metadata { get; set; } = new();
}

public class UpdateCharacterMapRequest
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public App.Domain.Models.CharacterMetadata? Metadata { get; set; }
}

// Badge Configuration API Models
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

public class UpdateBadgeConfigurationRequest
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public string? Axis { get; set; }
    public float? Threshold { get; set; }
    public string? ImageId { get; set; }
}

// Character Selection API Model
public class SelectCharacterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CharacterId { get; set; } = string.Empty;
}

// User Badge API Models
public class AwardBadgeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string UserProfileId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string BadgeConfigurationId { get; set; } = string.Empty;
    
    [Required]
    [Range(0.1, 100.0)]
    public float TriggerValue { get; set; }
    
    public string? GameSessionId { get; set; }
    public string? ScenarioId { get; set; }
}

public class UserBadgeResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserProfileId { get; set; } = string.Empty;
    public string BadgeConfigurationId { get; set; } = string.Empty;
    public string BadgeName { get; set; } = string.Empty;
    public string BadgeMessage { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public float TriggerValue { get; set; }
    public float Threshold { get; set; }
    public DateTime EarnedAt { get; set; }
    public string? GameSessionId { get; set; }
    public string? ScenarioId { get; set; }
    public string ImageId { get; set; } = string.Empty;
}

// Profile Management API Models
public class CreateMultipleProfilesRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(10)] // Reasonable limit for onboarding
    public List<CreateUserProfileRequest> Profiles { get; set; } = new();
}

public class ProfileAssignmentRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ProfileId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CharacterId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this profile should be treated as an NPC for this assignment
    /// </summary>
    public bool IsNpcAssignment { get; set; } = false;
}

public class UpdateProfileAccountRequest
{
    /// <summary>
    /// Email address of the account to associate the profile with
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Represents a scenario reference validation result
/// </summary>
public class ScenarioReferenceValidation
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioTitle { get; set; } = string.Empty;
    public List<MediaReference> MediaReferences { get; set; } = new();
    public List<CharacterReference> CharacterReferences { get; set; } = new();
    public List<MissingReference> MissingReferences { get; set; } = new();
    public bool HasMissingReferences => MissingReferences.Any();
    public int TotalReferences => MediaReferences.Count + CharacterReferences.Count;
    public int MissingReferencesCount => MissingReferences.Count;
}

/// <summary>
/// Represents a media reference in a scenario
/// </summary>
public class MediaReference
{
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string MediaId { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty; // image, audio, video
    public bool HasMetadata { get; set; }
    public bool MediaExists { get; set; }
}

/// <summary>
/// Represents a character reference in a scenario
/// </summary>
public class CharacterReference
{
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public bool HasMetadata { get; set; }
    public bool CharacterExists { get; set; }
}

/// <summary>
/// Represents a missing reference (media or character)
/// </summary>
public class MissingReference
{
    public string ReferenceId { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty; // media, character
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty; // missing_file, missing_metadata, invalid_reference
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request model for validating scenario references
/// </summary>
public class ValidateScenarioReferencesRequest
{
    public string ScenarioId { get; set; } = string.Empty;
    public bool IncludeMetadataValidation { get; set; } = true;
}

// Passwordless Signup Models
public class PasswordlessSignupRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

public class PasswordlessSignupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class PasswordlessVerifyRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

public class PasswordlessVerifyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Account? Account { get; set; }
    public string? Token { get; set; }
}

// Passwordless Signin Models
public class PasswordlessSigninRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class PasswordlessSigninResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Email { get; set; }
}