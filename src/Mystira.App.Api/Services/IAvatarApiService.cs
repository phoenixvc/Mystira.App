using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// </summary>
/// <remarks>
/// Migration guide:
/// - GetAvatarsAsync → GetAvatarsQuery
/// - GetAvatarsByAgeGroupAsync → GetAvatarsByAgeGroupQuery
/// - GetAvatarConfigurationFileAsync → (Create GetAvatarConfigurationFileQuery if needed)
/// - UpdateAvatarConfigurationFileAsync → (Create UpdateAvatarConfigurationFileCommand if needed)
/// - SetAvatarsForAgeGroupAsync → (Create SetAvatarsForAgeGroupCommand if needed)
/// See ARCHITECTURAL_REFACTORING_PLAN.md for details.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
public interface IAvatarApiService
{
    /// <summary>
    /// Gets all avatar configurations
    /// </summary>
    Task<AvatarResponse> GetAvatarsAsync();

    /// <summary>
    /// Gets avatars for a specific age group
    /// </summary>
    Task<AvatarConfigurationResponse?> GetAvatarsByAgeGroupAsync(string ageGroup);

    /// <summary>
    /// Gets the avatar configuration file
    /// </summary>
    Task<AvatarConfigurationFile?> GetAvatarConfigurationFileAsync();

    /// <summary>
    /// Updates the avatar configuration file
    /// </summary>
    Task<AvatarConfigurationFile> UpdateAvatarConfigurationFileAsync(AvatarConfigurationFile file);

    /// <summary>
    /// Sets avatars for a specific age group
    /// </summary>
    Task<AvatarConfigurationFile> SetAvatarsForAgeGroupAsync(string ageGroup, List<string> mediaIds);
}
