using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing avatar configurations
/// </summary>
public class AvatarApiService : IAvatarApiService
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AvatarApiService> _logger;

    public AvatarApiService(
        IAvatarConfigurationFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AvatarApiService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all avatar configurations
    /// </summary>
    public async Task<AvatarResponse> GetAvatarsAsync()
    {
        try
        {
            var configFile = await GetAvatarConfigurationFileAsync();

            var response = new AvatarResponse
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
    public async Task<AvatarConfigurationResponse?> GetAvatarsByAgeGroupAsync(string ageGroup)
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
                return new AvatarConfigurationResponse
                {
                    AgeGroup = ageGroup,
                    AvatarMediaIds = new List<string>()
                };
            }

            return new AvatarConfigurationResponse
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
            return await _repository.GetAsync();
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

            var result = await _repository.AddOrUpdateAsync(file);
            await _unitOfWork.SaveChangesAsync();
            return result;
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

            return await UpdateAvatarConfigurationFileAsync(configFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting avatars for age group: {AgeGroup}", ageGroup);
            throw;
        }
    }
}
