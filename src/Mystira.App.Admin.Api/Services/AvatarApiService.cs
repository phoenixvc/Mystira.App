using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;
using ContractsAvatarResponse = Mystira.App.Contracts.Responses.Media.AvatarResponse;
using ContractsAvatarConfigurationResponse = Mystira.App.Contracts.Responses.Media.AvatarConfigurationResponse;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service for managing avatar configurations (Admin)
/// </summary>
public class AvatarApiService : IAvatarApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<AvatarApiService> _logger;

    public AvatarApiService(MystiraAppDbContext context, ILogger<AvatarApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all avatar configurations
    /// </summary>
    public async Task<ContractsAvatarResponse> GetAvatarsAsync()
    {
        try
        {
            var configFile = await GetAvatarConfigurationFileAsync();

            var response = new ContractsAvatarResponse
            {
                AgeGroupAvatars = configFile?.AgeGroupAvatars ?? new Dictionary<string, List<string>>()
            };

            // Ensure all age groups are present
            foreach (var ageGroup in AgeGroupConstants.AllAgeGroups)
            {
                if (!response.AgeGroupAvatars.ContainsKey(ageGroup))
                {
                    response.AgeGroupAvatars[ageGroup] = new List<string>();
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatars");
            throw;
        }
    }

    /// <summary>
    /// Gets avatars for a specific age group
    /// </summary>
    public async Task<ContractsAvatarConfigurationResponse?> GetAvatarsByAgeGroupAsync(string ageGroup)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                _logger.LogWarning("Age group is required");
                return null;
            }

            var configFile = await GetAvatarConfigurationFileAsync();

            if (configFile == null || !configFile.AgeGroupAvatars.TryGetValue(ageGroup, out var avatars))
            {
                return new ContractsAvatarConfigurationResponse
                {
                    AgeGroup = ageGroup,
                    AvatarMediaIds = new List<string>()
                };
            }

            return new ContractsAvatarConfigurationResponse
            {
                AgeGroup = ageGroup,
                AvatarMediaIds = avatars
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatars for age group: {AgeGroup}", ageGroup);
            throw;
        }
    }

    /// <summary>
    /// Gets the avatar configuration file
    /// </summary>
    public async Task<AvatarConfigurationFile?> GetAvatarConfigurationFileAsync()
    {
        try
        {
            return await _context.AvatarConfigurationFiles.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving avatar configuration file");
            throw;
        }
    }

    /// <summary>
    /// Updates the avatar configuration file
    /// </summary>
    public async Task<AvatarConfigurationFile> UpdateAvatarConfigurationFileAsync(AvatarConfigurationFile file)
    {
        try
        {
            file.UpdatedAt = DateTime.UtcNow;

            var existingFile = await _context.AvatarConfigurationFiles.FirstOrDefaultAsync();
            if (existingFile != null)
            {
                _context.Entry(existingFile).CurrentValues.SetValues(file);
                existingFile.AgeGroupAvatars = file.AgeGroupAvatars;
                // Mark the complex property as modified so EF Core recognizes the change
                _context.Entry(existingFile).Property(e => e.AgeGroupAvatars).IsModified = true;
            }
            else
            {
                await _context.AvatarConfigurationFiles.AddAsync(file);
            }

            await _context.SaveChangesAsync();
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating avatar configuration file");
            throw;
        }
    }

    /// <summary>
    /// Sets avatars for a specific age group
    /// </summary>
    public async Task<AvatarConfigurationFile> SetAvatarsForAgeGroupAsync(string ageGroup, List<string> mediaIds)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                throw new ArgumentException("Age group is required", nameof(ageGroup));
            }

            var configFile = await GetAvatarConfigurationFileAsync() ?? new AvatarConfigurationFile();

            if (configFile.AgeGroupAvatars == null)
            {
                configFile.AgeGroupAvatars = new Dictionary<string, List<string>>();
            }

            configFile.AgeGroupAvatars[ageGroup] = mediaIds ?? new List<string>();

            _logger.LogInformation("Set {Count} avatars for age group: {AgeGroup}", mediaIds?.Count ?? 0, ageGroup);
            return await UpdateAvatarConfigurationFileAsync(configFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting avatars for age group: {AgeGroup}", ageGroup);
            throw;
        }
    }

    /// <summary>
    /// Adds an avatar to a specific age group
    /// </summary>
    public async Task<AvatarConfigurationFile> AddAvatarToAgeGroupAsync(string ageGroup, string mediaId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                throw new ArgumentException("Age group is required", nameof(ageGroup));
            }

            if (string.IsNullOrWhiteSpace(mediaId))
            {
                throw new ArgumentException("Media ID is required", nameof(mediaId));
            }

            var configFile = await GetAvatarConfigurationFileAsync() ?? new AvatarConfigurationFile();

            if (configFile.AgeGroupAvatars == null)
            {
                configFile.AgeGroupAvatars = new Dictionary<string, List<string>>();
            }

            if (!configFile.AgeGroupAvatars.ContainsKey(ageGroup))
            {
                configFile.AgeGroupAvatars[ageGroup] = new List<string>();
            }

            if (!configFile.AgeGroupAvatars[ageGroup].Contains(mediaId))
            {
                configFile.AgeGroupAvatars[ageGroup].Add(mediaId);
                _logger.LogInformation("Added avatar {MediaId} to age group: {AgeGroup}", mediaId, ageGroup);
            }
            else
            {
                _logger.LogWarning("Avatar {MediaId} already exists for age group: {AgeGroup}", mediaId, ageGroup);
            }

            return await UpdateAvatarConfigurationFileAsync(configFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding avatar to age group: {AgeGroup}", ageGroup);
            throw;
        }
    }

    /// <summary>
    /// Removes an avatar from a specific age group
    /// </summary>
    public async Task<AvatarConfigurationFile> RemoveAvatarFromAgeGroupAsync(string ageGroup, string mediaId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                throw new ArgumentException("Age group is required", nameof(ageGroup));
            }

            if (string.IsNullOrWhiteSpace(mediaId))
            {
                throw new ArgumentException("Media ID is required", nameof(mediaId));
            }

            var configFile = await GetAvatarConfigurationFileAsync() ?? new AvatarConfigurationFile();

            if (configFile.AgeGroupAvatars != null && configFile.AgeGroupAvatars.TryGetValue(ageGroup, out var avatars))
            {
                if (avatars.Remove(mediaId))
                {
                    _logger.LogInformation("Removed avatar {MediaId} from age group: {AgeGroup}", mediaId, ageGroup);
                }
                else
                {
                    _logger.LogWarning("Avatar {MediaId} not found in age group: {AgeGroup}", mediaId, ageGroup);
                }
            }

            return await UpdateAvatarConfigurationFileAsync(configFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing avatar from age group: {AgeGroup}", ageGroup);
            throw;
        }
    }
}
