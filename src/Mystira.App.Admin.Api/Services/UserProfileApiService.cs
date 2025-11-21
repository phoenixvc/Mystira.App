using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Data;
using Microsoft.Extensions.Logging;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Shared.Models;
using Mystira.App.Shared.Services;

namespace Mystira.App.Admin.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly UserProfileService<MystiraAppDbContext> _userProfileService;

    public UserProfileApiService(MystiraAppDbContext context, ILogger<UserProfileService<MystiraAppDbContext>> logger)
    {
        _userProfileService = new UserProfileService<MystiraAppDbContext>(context, logger);
    }

    private static Mystira.App.Shared.Models.CreateUserProfileRequest MapToShared(Mystira.App.Admin.Api.Models.CreateUserProfileRequest req) => new()
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

    private static Mystira.App.Shared.Models.UpdateUserProfileRequest MapToShared(Mystira.App.Admin.Api.Models.UpdateUserProfileRequest req) => new()
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

    private static Mystira.App.Shared.Models.CreateGuestProfileRequest MapToShared(Mystira.App.Admin.Api.Models.CreateGuestProfileRequest req) => new()
    {
        Name = req.Name,
        AgeGroup = req.AgeGroup,
        UseAdjectiveNames = req.UseAdjectiveNames
    };

    private static Mystira.App.Shared.Models.CreateMultipleProfilesRequest MapToShared(Mystira.App.Admin.Api.Models.CreateMultipleProfilesRequest req) => new()
    {
        Profiles = req.Profiles.Select(MapToShared).ToList()
    };

    public async Task<UserProfile> CreateProfileAsync(Mystira.App.Admin.Api.Models.CreateUserProfileRequest request) => await _userProfileService.CreateProfileAsync(MapToShared(request));
    public async Task<UserProfile> CreateGuestProfileAsync(Mystira.App.Admin.Api.Models.CreateGuestProfileRequest request) => await _userProfileService.CreateGuestProfileAsync(MapToShared(request));
    public async Task<List<UserProfile>> CreateMultipleProfilesAsync(Mystira.App.Admin.Api.Models.CreateMultipleProfilesRequest request) => await _userProfileService.CreateMultipleProfilesAsync(MapToShared(request));
    public async Task<UserProfile?> GetProfileAsync(string name) => await _userProfileService.GetProfileAsync(name);
    public async Task<UserProfile?> GetProfileByIdAsync(string id) => await _userProfileService.GetProfileByIdAsync(id);
    public async Task<UserProfile?> UpdateProfileAsync(string name, Mystira.App.Admin.Api.Models.UpdateUserProfileRequest request) => await _userProfileService.UpdateProfileAsync(name, MapToShared(request));
    public async Task<UserProfile?> UpdateProfileByIdAsync(string id, Mystira.App.Admin.Api.Models.UpdateUserProfileRequest request) => await _userProfileService.UpdateProfileByIdAsync(id, MapToShared(request));
    public async Task<bool> DeleteProfileAsync(string name) => await _userProfileService.DeleteProfileAsync(name);
    public async Task<bool> CompleteOnboardingAsync(string name) => await _userProfileService.CompleteOnboardingAsync(name);
    public async Task<List<UserProfile>> GetAllProfilesAsync() => await _userProfileService.GetAllProfilesAsync();
    public async Task<List<UserProfile>> GetNonGuestProfilesAsync() => await _userProfileService.GetNonGuestProfilesAsync();
    public async Task<List<UserProfile>> GetGuestProfilesAsync() => await _userProfileService.GetGuestProfilesAsync();
    public async Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false) => await _userProfileService.AssignCharacterToProfileAsync(profileId, characterId, isNpc);
}
