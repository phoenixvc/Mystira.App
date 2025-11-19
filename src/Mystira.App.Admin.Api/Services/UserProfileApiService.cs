
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using System.Linq;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Shared.Services;

namespace Mystira.App.Admin.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly IUserProfileService _userProfileService;

    public UserProfileApiService(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    public async Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request) => await _userProfileService.CreateProfileAsync(request);

    public async Task<UserProfile> CreateGuestProfileAsync(CreateGuestProfileRequest request) => await _userProfileService.CreateGuestProfileAsync(request);

    public async Task<List<UserProfile>> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request) => await _userProfileService.CreateMultipleProfilesAsync(request);

    public async Task<UserProfile?> GetProfileAsync(string name) => await _userProfileService.GetProfileAsync(name);

    public async Task<UserProfile?> GetProfileByIdAsync(string id) => await _userProfileService.GetProfileByIdAsync(id);

    public async Task<UserProfile?> UpdateProfileAsync(string name, UpdateUserProfileRequest request) => await _userProfileService.UpdateProfileAsync(name, request);

    public async Task<bool> DeleteProfileAsync(string name) => await _userProfileService.DeleteProfileAsync(name);

    public async Task<bool> CompleteOnboardingAsync(string name) => await _userProfileService.CompleteOnboardingAsync(name);

    public async Task<List<UserProfile>> GetAllProfilesAsync() => await _userProfileService.GetAllProfilesAsync();

    public async Task<List<UserProfile>> GetNonGuestProfilesAsync() => await _userProfileService.GetNonGuestProfilesAsync();

    public async Task<List<UserProfile>> GetGuestProfilesAsync() => await _userProfileService.GetGuestProfilesAsync();
    public async Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false) => await _userProfileService.AssignCharacterToProfileAsync(profileId, characterId, isNpc);
}
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

    public async Task<UserProfile?> GetProfileAsync(string name)
    {
        return await _context.UserProfiles
            .Include(p => p.EarnedBadges)
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<UserProfile?> GetProfileByIdAsync(string id)
    {
        return await _context.UserProfiles
            .Include(p => p.EarnedBadges)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<UserProfile?> UpdateProfileAsync(string name, UpdateUserProfileRequest request)
     {
         var profile = await GetProfileAsync(name);
         if (profile == null)
             return null;

         // Apply updates
         if (request.PreferredFantasyThemes != null)
         {
             // Validate fantasy themes
             var invalidThemes = request.PreferredFantasyThemes.Except(FantasyThemes.Available).ToList();
             if (invalidThemes.Any())
                 throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");

             profile.PreferredFantasyThemes = request.PreferredFantasyThemes;
         }

         if (request.AgeGroup != null)
         {
             // Validate age group
             if (!AgeGroup.IsValid(request.AgeGroup))
                 throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroup.All.Select(a => a.Name))}");

             profile.AgeGroup = request.AgeGroup;
         }

         if (request.DateOfBirth.HasValue)
         {
             profile.DateOfBirth = request.DateOfBirth;
             // Update age group automatically if date of birth is provided
             profile.UpdateAgeGroupFromBirthDate();
         }

         if (request.HasCompletedOnboarding.HasValue)
             profile.HasCompletedOnboarding = request.HasCompletedOnboarding.Value;

         if (request.IsGuest.HasValue)
             profile.IsGuest = request.IsGuest.Value;

         if (request.IsNpc.HasValue)
             profile.IsNpc = request.IsNpc.Value;

         if (request.AccountId != null)
             profile.AccountId = request.AccountId;

         profile.UpdatedAt = DateTime.UtcNow;
         await _context.SaveChangesAsync();

         _logger.LogInformation("Updated user profile: {Name}", profile.Name);
         return profile;
     }

    public async Task<UserProfile?> UpdateProfileByIdAsync(string id, UpdateUserProfileRequest request)
    {
        var profile = await GetProfileByIdAsync(id);
        if (profile == null)
            return null;

        // Apply updates
        if (request.PreferredFantasyThemes != null)
        {
            // Validate fantasy themes
            var invalidThemes = request.PreferredFantasyThemes.Except(FantasyThemes.Available).ToList();
            if (invalidThemes.Any())
                throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");

            profile.PreferredFantasyThemes = request.PreferredFantasyThemes;
        }

        if (request.AgeGroup != null)
        {
            // Validate age group
            if (!AgeGroup.IsValid(request.AgeGroup))
                throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroup.All.Select(a => a.Name))}");

            profile.AgeGroup = request.AgeGroup;
        }

        if (request.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = request.DateOfBirth;
            // Update age group automatically if date of birth is provided
            profile.UpdateAgeGroupFromBirthDate();
        }

        if (request.HasCompletedOnboarding.HasValue)
            profile.HasCompletedOnboarding = request.HasCompletedOnboarding.Value;

        if (request.IsGuest.HasValue)
            profile.IsGuest = request.IsGuest.Value;

        if (request.IsNpc.HasValue)
            profile.IsNpc = request.IsNpc.Value;

        if (request.AccountId != null)
            profile.AccountId = request.AccountId;

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user profile by ID: {Id}", id);
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(string name)
    {
        var profile = await GetProfileAsync(name);
        if (profile == null)
            return false;

        // COPPA compliance: Also delete associated sessions, badges, and data
        var sessions = await _context.GameSessions
            .Where(s => s.ProfileId == profile.Id)
            .ToListAsync();

        var badges = await _context.UserBadges
            .Where(b => b.UserProfileId == profile.Id)
            .ToListAsync();

        _context.GameSessions.RemoveRange(sessions);
        _context.UserBadges.RemoveRange(badges);
        _context.UserProfiles.Remove(profile);
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted user profile and associated data: {Name} (badges: {BadgeCount})", 
            name, badges.Count);
        return true;
    }

    public async Task<bool> CompleteOnboardingAsync(string name)
    {
        var profile = await GetProfileAsync(name);
        if (profile == null)
            return false;

        profile.HasCompletedOnboarding = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Completed onboarding for user: {Name}", name);
        return true;
    }

    public async Task<List<UserProfile>> GetAllProfilesAsync()
    {
        return await _context.UserProfiles
            .Include(p => p.EarnedBadges)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<UserProfile>> GetNonGuestProfilesAsync()
    {
        return await _context.UserProfiles
            .Include(p => p.EarnedBadges)
            .Where(p => !p.IsGuest)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<UserProfile>> GetGuestProfilesAsync()
    {
        return await _context.UserProfiles
            .Include(p => p.EarnedBadges)
            .Where(p => p.IsGuest)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.Id == profileId);
        if (profile == null)
            return false;

        // Check if character exists
        var character = await _context.CharacterMaps.FirstOrDefaultAsync(c => c.Id == characterId);
        if (character == null)
            return false;

        // This is a conceptual assignment - in practice, this would be stored in a game session
        // or a separate assignment table. For now, we'll log it and return success.
        profile.IsNpc = isNpc;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned character {CharacterId} to profile {ProfileId} (NPC: {IsNPC})", 
            characterId, profileId, isNpc);

        return true;
    }
}
>>>>>>> origin/main
