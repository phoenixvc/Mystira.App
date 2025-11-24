using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.UseCases.UserProfiles;
using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly IUserProfileRepository _repository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ICharacterMapRepository _characterMapRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserProfileApiService> _logger;
    private readonly CreateUserProfileUseCase _createUserProfileUseCase;
    private readonly UpdateUserProfileUseCase _updateUserProfileUseCase;
    private readonly GetUserProfileUseCase _getUserProfileUseCase;
    private readonly DeleteUserProfileUseCase _deleteUserProfileUseCase;

    public UserProfileApiService(
        IUserProfileRepository repository,
        IGameSessionRepository gameSessionRepository,
        ICharacterMapRepository characterMapRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserProfileApiService> logger,
        CreateUserProfileUseCase createUserProfileUseCase,
        UpdateUserProfileUseCase updateUserProfileUseCase,
        GetUserProfileUseCase getUserProfileUseCase,
        DeleteUserProfileUseCase deleteUserProfileUseCase)
    {
        _repository = repository;
        _gameSessionRepository = gameSessionRepository;
        _characterMapRepository = characterMapRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createUserProfileUseCase = createUserProfileUseCase;
        _updateUserProfileUseCase = updateUserProfileUseCase;
        _getUserProfileUseCase = getUserProfileUseCase;
        _deleteUserProfileUseCase = deleteUserProfileUseCase;
    }

    public async Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request)
    {
        return await _createUserProfileUseCase.ExecuteAsync(request);
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
        return await _getUserProfileUseCase.ExecuteAsync(id);
    }

    public async Task<UserProfile?> UpdateProfileByIdAsync(string id, UpdateUserProfileRequest request)
    {
        var profile = await _updateUserProfileUseCase.ExecuteAsync(id, request);
        if (profile != null && request.SelectedAvatarMediaId != null)
        {
            // Keep AvatarMediaId in sync with SelectedAvatarMediaId (service-specific logic)
            profile.AvatarMediaId = request.SelectedAvatarMediaId;
            await _repository.UpdateAsync(profile);
            await _unitOfWork.SaveChangesAsync();
        }
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(string id)
    {
        return await _deleteUserProfileUseCase.ExecuteAsync(id);
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
