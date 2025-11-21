using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Data;
using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly IUserProfileRepository _repository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ICharacterMapRepository _characterMapRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserProfileApiService> _logger;

    public UserProfileApiService(
        IUserProfileRepository repository,
        IGameSessionRepository gameSessionRepository,
        ICharacterMapRepository characterMapRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserProfileApiService> logger)
    {
        _repository = repository;
        _gameSessionRepository = gameSessionRepository;
        _characterMapRepository = characterMapRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request)
    {
        // Check if profile already exists
        var existingProfile = await GetProfileByIdAsync(request.Id);
        if (existingProfile != null)
        {
            throw new ArgumentException($"Profile already exists for name: {request.Name}");
        }

        // Validate fantasy themes
        var invalidThemes = request.PreferredFantasyThemes.Where(t => FantasyTheme.Parse(t) == null).ToList();
        if (invalidThemes.Any())
        {
            throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");
        }

        // Validate age group
        if (!AgeGroupConstants.AllAgeGroups.Contains(request.AgeGroup))
        {
            throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroupConstants.AllAgeGroups)}");
        }

        var profile = new UserProfile
        {
            Name = request.Name,
            AccountId = request.AccountId,
            PreferredFantasyThemes = request.PreferredFantasyThemes?.Select(t => FantasyTheme.Parse(t)!).ToList() ?? new List<FantasyTheme>(),
            AgeGroupName = request.AgeGroup,
            DateOfBirth = request.DateOfBirth,
            IsGuest = request.IsGuest,
            IsNpc = request.IsNpc,
            HasCompletedOnboarding = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AvatarMediaId = request.SelectedAvatarMediaId,
            SelectedAvatarMediaId = request.SelectedAvatarMediaId
        };

        // If date of birth is provided, update age group automatically
        if (profile.DateOfBirth.HasValue)
        {
            profile.UpdateAgeGroupFromBirthDate();
        }

        await _repository.AddAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created new user profile: {Name} (Guest: {IsGuest}, NPC: {IsNPC})",
            profile.Name, profile.IsGuest, profile.IsNpc);
        return profile;
    }

    public async Task<UserProfile> CreateGuestProfileAsync(CreateGuestProfileRequest request)
    {
        // Generate random name if not provided
        var name = !string.IsNullOrEmpty(request.Name)
            ? request.Name
            : RandomNameGenerator.GenerateGuestName(request.UseAdjectiveNames);

        // Ensure name is unique for guest profiles
        var baseName = name;
        var counter = 1;
        while (await GetProfileByIdAsync(request.Id) != null)
        {
            name = $"{baseName} {counter}";
            counter++;
        }

        // Validate age group
        if (!AgeGroupConstants.AllAgeGroups.Contains(request.AgeGroup))
        {
            throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroupConstants.AllAgeGroups)}");
        }

        var profile = new UserProfile
        {
            Name = name,
            PreferredFantasyThemes = new List<FantasyTheme>(), // Empty for guest profiles
            AgeGroupName = request.AgeGroup,
            IsGuest = true,
            IsNpc = false,
            HasCompletedOnboarding = true, // Guests don't need onboarding
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created guest profile: {Name}", profile.Name);
        return profile;
    }

    public async Task<List<UserProfile>> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request)
    {
        var createdProfiles = new List<UserProfile>();

        foreach (var profileRequest in request.Profiles)
        {
            try
            {
                var profile = await CreateProfileAsync(profileRequest);
                createdProfiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create profile {Name} in batch", profileRequest.Name);
                // Continue with other profiles
            }
        }

        _logger.LogInformation("Created {Count} profiles in batch", createdProfiles.Count);
        return createdProfiles;
    }

    public async Task<UserProfile?> GetProfileByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<UserProfile?> UpdateProfileByIdAsync(string id, UpdateUserProfileRequest request)
    {
        var profile = await GetProfileByIdAsync(id);
        if (profile == null)
        {
            return null;
        }

        // Apply updates
        if (request.PreferredFantasyThemes != null)
        {
            // Validate fantasy themes
            var invalidThemes = request.PreferredFantasyThemes.Where(t => FantasyTheme.Parse(t) == null).ToList();
            if (invalidThemes.Any())
            {
                throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");
            }

            profile.PreferredFantasyThemes = request.PreferredFantasyThemes.Select(t => FantasyTheme.Parse(t)!).ToList();
        }

        if (request.AgeGroup != null)
        {
            // Validate age group
            if (!AgeGroupConstants.AllAgeGroups.Contains(request.AgeGroup))
            {
                throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroupConstants.AllAgeGroups)}");
            }

            profile.AgeGroupName = request.AgeGroup;
        }

        if (request.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = request.DateOfBirth;
            // Update age group automatically if date of birth is provided
            profile.UpdateAgeGroupFromBirthDate();
        }

        if (request.HasCompletedOnboarding.HasValue)
        {
            profile.HasCompletedOnboarding = request.HasCompletedOnboarding.Value;
        }

        if (request.IsGuest.HasValue)
        {
            profile.IsGuest = request.IsGuest.Value;
        }

        if (request.IsNpc.HasValue)
        {
            profile.IsNpc = request.IsNpc.Value;
        }

        if (request.AccountId != null)
        {
            profile.AccountId = request.AccountId;
        }

        if (request.SelectedAvatarMediaId != null)
        {
            profile.SelectedAvatarMediaId = request.SelectedAvatarMediaId;
            profile.AvatarMediaId = request.SelectedAvatarMediaId;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated user profile by ID: {Id}", id);
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(string id)
    {
        var profile = await GetProfileByIdAsync(id);
        if (profile == null)
        {
            return false;
        }

        // COPPA compliance: Also delete associated sessions, badges, and data
        var sessions = await _gameSessionRepository.GetByProfileIdAsync(profile.Id);
        foreach (var session in sessions)
        {
            await _gameSessionRepository.DeleteAsync(session.Id);
        }

        await _repository.DeleteAsync(profile.Id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted user profile and associated data: {Name}", profile.Name);
        return true;
    }

    public async Task<bool> CompleteOnboardingAsync(string id)
    {
        var profile = await GetProfileByIdAsync(id);
        if (profile == null)
        {
            return false;
        }

        profile.HasCompletedOnboarding = true;
        await _repository.UpdateAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Completed onboarding for user: {Name}", profile.Name);
        return true;
    }

    public async Task<List<UserProfile>> GetAllProfilesAsync()
    {
        var profiles = await _repository.GetAllAsync();
        return profiles.OrderBy(p => p.Name).ToList();
    }

    public async Task<List<UserProfile>> GetNonGuestProfilesAsync()
    {
        var profiles = await _repository.GetNonGuestProfilesAsync();
        return profiles.ToList();
    }

    public async Task<List<UserProfile>> GetGuestProfilesAsync()
    {
        var profiles = await _repository.GetGuestProfilesAsync();
        return profiles.ToList();
    }

    public async Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false)
    {
        var profile = await _repository.GetByIdAsync(profileId);
        if (profile == null)
        {
            return false;
        }

        // Check if character exists
        var character = await _characterMapRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            return false;
        }

        // This is a conceptual assignment - in practice, this would be stored in a game session
        // or a separate assignment table. For now, we'll log it and return success.
        profile.IsNpc = isNpc;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Assigned character {CharacterId} to profile {ProfileId} (NPC: {IsNPC})",
            characterId, profileId, isNpc);

        return true;
    }
}
