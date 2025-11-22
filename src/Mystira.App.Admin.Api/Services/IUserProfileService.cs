using System.Collections.Generic;
using System.Threading.Tasks;
using Mystira.App.Domain.Models;
using Mystira.App.Shared.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IUserProfileService
{
    Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request);
    Task<UserProfile> GetProfileAsync(string name);
    Task<List<UserProfile>> GetAllProfilesAsync();
    Task<List<UserProfile>> GetGuestProfilesAsync();
    Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false);
    // Add other public methods from UserProfileService as needed
}
