namespace Mystira.App.PWA.Services;

public interface IAvatarApiClient
{
    Task<Dictionary<string, List<string>>?> GetAvatarsAsync();
    Task<List<string>?> GetAvatarsByAgeGroupAsync(string ageGroup);
}

