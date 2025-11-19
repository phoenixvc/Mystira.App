using Mystira.App.Domain.Models;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Data;
using Microsoft.Extensions.Logging;
using Mystira.App.Admin.Api.Services;

namespace Mystira.App.Admin.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly UserProfileService _userProfileService;

    public UserProfileApiService(MystiraAppDbContext context, ILogger<UserProfileService> logger)
    {
        _userProfileService = new UserProfileService(context, logger);
    }

    private static Mystira.App.Shared.Models.CreateUserProfileRequest MapToShared(CreateUserProfileRequest req) => new()
    {
        Name = req.Name,
        PreferredFantasyThemes = req.PreferredFantasyThemes,
        AgeGroup = req.AgeGroup,
        DateOfBirth = req.DateOfBirth,
        IsGuest = req.IsGuest,
        IsNpc = req.IsNpc,
        AccountId = req.AccountId,
        HasCompletedOnboarding = req.HasCompletedOnboarding
    };

    private static Mystira.App.Shared.Models.UpdateUserProfileRequest MapToShared(UpdateUserProfileRequest req) => new()
    {
        PreferredFantasyThemes = req.PreferredFantasyThemes,
        AgeGroup = req.AgeGroup,
        DateOfBirth = req.DateOfBirth,
        HasCompletedOnboarding = req.HasCompletedOnboarding,
        IsGuest = req.IsGuest,
        IsNpc = req.IsNpc,
        AccountId = req.AccountId,
        Pronouns = req.Pronouns,
        Bio = req.Bio
    };

    private static Mystira.App.Shared.Models.CreateGuestProfileRequest MapToShared(CreateGuestProfileRequest req) => new()
    {
        Name = req.Name,
        AgeGroup = req.AgeGroup,
        UseAdjectiveNames = req.UseAdjectiveNames
    };

    private static Mystira.App.Shared.Models.CreateMultipleProfilesRequest MapToShared(CreateMultipleProfilesRequest req) => new()
    {
        Profiles = req.Profiles.Select(MapToShared).ToList()
    };

    public async Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request) => await _userProfileService.CreateProfileAsync(MapToShared(request));
    public async Task<UserProfile> CreateGuestProfileAsync(CreateGuestProfileRequest request) => await _userProfileService.CreateGuestProfileAsync(MapToShared(request));
    public async Task<List<UserProfile>> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request) => await _userProfileService.CreateMultipleProfilesAsync(MapToShared(request));
    public async Task<UserProfile?> GetProfileAsync(string name) => await _userProfileService.GetProfileAsync(name);
    public async Task<UserProfile?> GetProfileByIdAsync(string id) => await _userProfileService.GetProfileByIdAsync(id);
    public async Task<UserProfile?> UpdateProfileAsync(string name, UpdateUserProfileRequest request) => await _userProfileService.UpdateProfileAsync(name, MapToShared(request));
    public async Task<UserProfile?> UpdateProfileByIdAsync(string id, UpdateUserProfileRequest request) => await _userProfileService.UpdateProfileByIdAsync(id, MapToShared(request));
    public async Task<bool> DeleteProfileAsync(string name) => await _userProfileService.DeleteProfileAsync(name);
    public async Task<bool> CompleteOnboardingAsync(string name) => await _userProfileService.CompleteOnboardingAsync(name);
    public async Task<List<UserProfile>> GetAllProfilesAsync() => await _userProfileService.GetAllProfilesAsync();
    public async Task<List<UserProfile>> GetNonGuestProfilesAsync() => await _userProfileService.GetNonGuestProfilesAsync();
    public async Task<List<UserProfile>> GetGuestProfilesAsync() => await _userProfileService.GetGuestProfilesAsync();
    public async Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false) => await _userProfileService.AssignCharacterToProfileAsync(profileId, characterId, isNpc);
}