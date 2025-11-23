using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

public class UserBadgeApiService : IUserBadgeApiService
{
    private readonly IUserBadgeRepository _repository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBadgeConfigurationApiService _badgeConfigService;
    private readonly ILogger<UserBadgeApiService> _logger;

    public UserBadgeApiService(
        IUserBadgeRepository repository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IBadgeConfigurationApiService badgeConfigService,
        ILogger<UserBadgeApiService> logger)
    {
        _repository = repository;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _badgeConfigService = badgeConfigService;
        _logger = logger;
    }

    public async Task<UserBadge> AwardBadgeAsync(AwardBadgeRequest request)
    {
        try
        {
            // Check if user already has this badge
            var existingBadge = await _repository.GetByUserProfileIdAndBadgeConfigIdAsync(
                request.UserProfileId, request.BadgeConfigurationId);

            if (existingBadge != null)
            {
                _logger.LogWarning("User {UserProfileId} already has badge {BadgeId}",
                    request.UserProfileId, request.BadgeConfigurationId);
                return existingBadge;
            }

            // Get badge configuration
            var badgeConfig = await _badgeConfigService.GetBadgeConfigurationAsync(request.BadgeConfigurationId);
            if (badgeConfig == null)
            {
                throw new ArgumentException($"Badge configuration not found: {request.BadgeConfigurationId}");
            }

            // Verify user profile exists
            var userProfile = await _userProfileRepository.GetByIdAsync(request.UserProfileId);
            if (userProfile == null)
            {
                throw new ArgumentException($"User profile not found: {request.UserProfileId}");
            }

            // Create new badge
            var newBadge = new UserBadge
            {
                UserProfileId = request.UserProfileId,
                BadgeConfigurationId = request.BadgeConfigurationId,
                BadgeName = badgeConfig.Name,
                BadgeMessage = badgeConfig.Message,
                Axis = badgeConfig.Axis,
                TriggerValue = request.TriggerValue,
                Threshold = badgeConfig.Threshold,
                GameSessionId = request.GameSessionId,
                ScenarioId = request.ScenarioId,
                ImageId = badgeConfig.ImageId,
                EarnedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(newBadge);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Awarded badge {BadgeName} to user {UserProfileId}",
                badgeConfig.Name, request.UserProfileId);

            return newBadge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding badge {BadgeId} to user {UserProfileId}",
                request.BadgeConfigurationId, request.UserProfileId);
            throw;
        }
    }

    public async Task<List<UserBadge>> GetUserBadgesAsync(string userProfileId)
    {
        try
        {
            return (await _repository.GetByUserProfileIdAsync(userProfileId)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for user {UserProfileId}", userProfileId);
            throw;
        }
    }

    public async Task<List<UserBadge>> GetUserBadgesForAxisAsync(string userProfileId, string axis)
    {
        try
        {
            var badges = await _repository.GetByUserProfileIdAndAxisAsync(userProfileId, axis);
            return badges
                .Where(b => b.Axis.Equals(axis, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for user {UserProfileId} and axis {Axis}",
                userProfileId, axis);
            throw;
        }
    }

    public async Task<bool> HasUserEarnedBadgeAsync(string userProfileId, string badgeConfigurationId)
    {
        try
        {
            var badge = await _repository.GetByUserProfileIdAndBadgeConfigIdAsync(userProfileId, badgeConfigurationId);
            return badge != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserProfileId} has badge {BadgeId}",
                userProfileId, badgeConfigurationId);
            throw;
        }
    }

    public async Task<bool> RemoveBadgeAsync(string userProfileId, string badgeId)
    {
        try
        {
            var badge = await _repository.GetByIdAsync(badgeId);
            if (badge == null || badge.UserProfileId != userProfileId)
            {
                return false;
            }

            await _repository.DeleteAsync(badgeId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Removed badge {BadgeId} from user {UserProfileId}",
                badgeId, userProfileId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing badge {BadgeId} from user {UserProfileId}",
                badgeId, userProfileId);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetBadgeStatisticsAsync(string userProfileId)
    {
        try
        {
            var badges = (await _repository.GetByUserProfileIdAsync(userProfileId)).ToList();

            var statistics = new Dictionary<string, int>
            {
                ["total"] = badges.Count
            };

            // Group by axis
            var axisCounts = badges.GroupBy(b => b.Axis)
                .ToDictionary(g => g.Key.ToLower(), g => g.Count());

            foreach (var axisCount in axisCounts)
            {
                statistics[axisCount.Key] = axisCount.Value;
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge statistics for user {UserProfileId}", userProfileId);
            throw;
        }
    }
}
