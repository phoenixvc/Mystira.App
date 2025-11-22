using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Domain.Models;
using Mystira.App.Shared.Models;
using Mystira.App.Shared.Services;
using ContractsCreateGuestProfileRequest = Mystira.App.Contracts.Requests.UserProfiles.CreateGuestProfileRequest;
using ContractsCreateMultipleProfilesRequest = Mystira.App.Contracts.Requests.UserProfiles.CreateMultipleProfilesRequest;
using ContractsCreateUserProfileRequest = Mystira.App.Contracts.Requests.UserProfiles.CreateUserProfileRequest;
using ContractsUpdateUserProfileRequest = Mystira.App.Contracts.Requests.UserProfiles.UpdateUserProfileRequest;

namespace Mystira.App.Admin.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly UserProfileService<MystiraAppDbContext> _userProfileService;

    public UserProfileApiService(MystiraAppDbContext context, ILogger<UserProfileService<MystiraAppDbContext>> logger)
    {
        _userProfileService = new UserProfileService<MystiraAppDbContext>(context, logger);
    }

    private static Mystira.App.Shared.Models.CreateUserProfileRequest MapToShared(ContractsCreateUserProfileRequest req) => new()
    {
        Id = req.Id,
        Name = req.Name,
        PreferredFantasyThemes = req.PreferredFantasyThemes,
        AgeGroup = req.AgeGroup,
        DateOfBirth = req.DateOfBirth,
        IsGuest = req.IsGuest,
        IsNpc = req.IsNpc,
        AccountId = req.AccountId,
        HasCompletedOnboarding = req.HasCompletedOnboarding,
        SelectedAvatarMediaId = req.SelectedAvatarMediaId
    };

    private static Mystira.App.Shared.Models.UpdateUserProfileRequest MapToShared(ContractsUpdateUserProfileRequest req) => new()
    {
        PreferredFantasyThemes = req.PreferredFantasyThemes,
        AgeGroup = req.AgeGroup,
        DateOfBirth = req.DateOfBirth,
        HasCompletedOnboarding = req.HasCompletedOnboarding,
        IsGuest = req.IsGuest,
        IsNpc = req.IsNpc,
        AccountId = req.AccountId,
        Pronouns = req.Pronouns,
        Bio = req.Bio,
        SelectedAvatarMediaId = req.SelectedAvatarMediaId
    };

    private static Mystira.App.Shared.Models.CreateGuestProfileRequest MapToShared(ContractsCreateGuestProfileRequest req) => new()
    {
        Id = req.Id,
        Name = req.Name,
        AgeGroup = req.AgeGroup,
        UseAdjectiveNames = req.UseAdjectiveNames
    };

    private static Mystira.App.Shared.Models.CreateMultipleProfilesRequest MapToShared(ContractsCreateMultipleProfilesRequest req) => new()
    {
        Profiles = req.Profiles.Select(MapToShared).ToList()
    };

    public async Task<UserProfile> CreateProfileAsync(ContractsCreateUserProfileRequest request) => await _userProfileService.CreateProfileAsync(MapToShared(request));
    public async Task<UserProfile> CreateGuestProfileAsync(ContractsCreateGuestProfileRequest request) => await _userProfileService.CreateGuestProfileAsync(MapToShared(request));
    public async Task<List<UserProfile>> CreateMultipleProfilesAsync(ContractsCreateMultipleProfilesRequest request) => await _userProfileService.CreateMultipleProfilesAsync(MapToShared(request));
    public async Task<UserProfile?> GetProfileAsync(string name) => await _userProfileService.GetProfileAsync(name);
    public async Task<UserProfile?> GetProfileByIdAsync(string id) => await _userProfileService.GetProfileByIdAsync(id);
    public async Task<UserProfile?> UpdateProfileAsync(string name, ContractsUpdateUserProfileRequest request) => await _userProfileService.UpdateProfileAsync(name, MapToShared(request));
    public async Task<UserProfile?> UpdateProfileByIdAsync(string id, ContractsUpdateUserProfileRequest request) => await _userProfileService.UpdateProfileByIdAsync(id, MapToShared(request));
    public async Task<bool> DeleteProfileAsync(string name) => await _userProfileService.DeleteProfileAsync(name);
    public async Task<bool> CompleteOnboardingAsync(string name) => await _userProfileService.CompleteOnboardingAsync(name);
    public async Task<List<UserProfile>> GetAllProfilesAsync() => await _userProfileService.GetAllProfilesAsync();
    public async Task<List<UserProfile>> GetNonGuestProfilesAsync() => await _userProfileService.GetNonGuestProfilesAsync();
    public async Task<List<UserProfile>> GetGuestProfilesAsync() => await _userProfileService.GetGuestProfilesAsync();
    public async Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false) => await _userProfileService.AssignCharacterToProfileAsync(profileId, characterId, isNpc);
}
