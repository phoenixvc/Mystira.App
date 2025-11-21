
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

public class UserProfileApiService : IUserProfileApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<UserProfileApiService> _logger;

    public UserProfileApiService(MystiraAppDbContext context, ILogger<UserProfileApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request)
    {
        var existingProfile = await GetProfileAsync(request.Name);
        if (existingProfile != null)
        {
            throw new ArgumentException($"Profile already exists for name: {request.Name}");
        }

        var invalidThemes = request.PreferredFantasyThemes.Where(t => FantasyTheme.Parse(t) == null).ToList();
        if (invalidThemes.Any())
        {
            throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");
        }

        if (AgeGroup.Parse(request.AgeGroup) == null)
        {
            throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", StringEnum<AgeGroup>.ValueMap.Keys)}");
        }

        var profile = new UserProfile
        {
            Name = request.Name,
            PreferredFantasyThemes = request.PreferredFantasyThemes.Select(t => FantasyTheme.Parse(t)!).ToList(),
            AgeGroupName = request.AgeGroup,
            DateOfBirth = request.DateOfBirth,
            IsGuest = request.IsGuest,
            IsNpc = request.IsNpc,
            HasCompletedOnboarding = request.HasCompletedOnboarding,
            AccountId = request.AccountId,
            Pronouns = request.Pronouns,
            Bio = request.Bio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (profile.DateOfBirth.HasValue)
        {
            profile.UpdateAgeGroupFromBirthDate();
        }

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new user profile: {Name} (Guest: {IsGuest}, NPC: {IsNPC})", profile.Name, profile.IsGuest, profile.IsNpc);
        return profile;
    }

    public async Task<UserProfile> CreateGuestProfileAsync(CreateGuestProfileRequest request)
    {
        var name = !string.IsNullOrEmpty(request.Name)
            ? request.Name
            : RandomNameGenerator.GenerateGuestName(request.UseAdjectiveNames);

        var baseName = name;
        var counter = 1;
        while (await GetProfileAsync(name) != null)
        {
            name = $"{baseName} {counter}";
            counter++;
        }

        if (AgeGroup.Parse(request.AgeGroup) == null)
        {
            throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", StringEnum<AgeGroup>.ValueMap.Keys)}");
        }

        var profile = new UserProfile
        {
            Name = name,
            PreferredFantasyThemes = new List<FantasyTheme>(),
            AgeGroupName = request.AgeGroup,
            IsGuest = true,
            IsNpc = false,
            HasCompletedOnboarding = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

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
        {
            return null;
        }

        if (request.PreferredFantasyThemes != null)
        {
            var invalidThemes = request.PreferredFantasyThemes.Where(t => FantasyTheme.Parse(t) == null).ToList();
            if (invalidThemes.Any())
            {
                throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");
            }

            profile.PreferredFantasyThemes = request.PreferredFantasyThemes.Select(t => FantasyTheme.Parse(t)!).ToList();
        }

        if (request.AgeGroup != null)
        {
            if (AgeGroup.Parse(request.AgeGroup) == null)
            {
                throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", StringEnum<AgeGroup>.ValueMap.Keys)}");
            }

            profile.AgeGroupName = request.AgeGroup;
        }

        if (request.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = request.DateOfBirth;
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

        if (request.Pronouns != null)
        {
            profile.Pronouns = request.Pronouns;
        }

        if (request.Bio != null)
        {
            profile.Bio = request.Bio;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user profile: {Name}", profile.Name);
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(string name)
    {
        var profile = await GetProfileAsync(name);
        if (profile == null)
        {
            return false;
        }

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

        _logger.LogInformation("Deleted user profile and associated data: {Name} (badges: {BadgeCount})", name, badges.Count);
        return true;
    }

    public async Task<bool> CompleteOnboardingAsync(string name)
    {
        var profile = await GetProfileAsync(name);
        if (profile == null)
        {
            return false;
        }

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
        {
            return false;
        }

        var character = await _context.CharacterMaps.FirstOrDefaultAsync(c => c.Id == characterId);
        if (character == null)
        {
            return false;
        }

        profile.IsNpc = isNpc;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned character {CharacterId} to profile {ProfileId} (NPC: {IsNPC})", characterId, profileId, isNpc);
        return true;
    }
}
