using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service interface for managing avatar configurations
/// </summary>
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
