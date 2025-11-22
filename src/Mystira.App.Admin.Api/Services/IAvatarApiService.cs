using Mystira.App.Admin.Api.Models;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;
using ContractsAvatarResponse = Mystira.App.Contracts.Responses.Media.AvatarResponse;
using ContractsAvatarConfigurationResponse = Mystira.App.Contracts.Responses.Media.AvatarConfigurationResponse;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service interface for managing avatar configurations in admin operations
/// </summary>
public interface IAvatarApiService
{
    /// <summary>
    /// Gets all avatar configurations
    /// </summary>
    Task<ContractsAvatarResponse> GetAvatarsAsync();

    /// <summary>
    /// Gets avatars for a specific age group
    /// </summary>
    Task<ContractsAvatarConfigurationResponse?> GetAvatarsByAgeGroupAsync(string ageGroup);

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

    /// <summary>
    /// Adds an avatar to a specific age group
    /// </summary>
    Task<AvatarConfigurationFile> AddAvatarToAgeGroupAsync(string ageGroup, string mediaId);

    /// <summary>
    /// Removes an avatar from a specific age group
    /// </summary>
    Task<AvatarConfigurationFile> RemoveAvatarFromAgeGroupAsync(string ageGroup, string mediaId);
}
