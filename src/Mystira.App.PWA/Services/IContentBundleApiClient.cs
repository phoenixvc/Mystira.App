using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IContentBundleApiClient
{
    Task<List<ContentBundle>> GetBundlesAsync();
    Task<List<ContentBundle>> GetBundlesByAgeGroupAsync(string ageGroup);
}

