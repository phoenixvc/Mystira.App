using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public interface IContentBundleService
{
    Task<List<ContentBundle>> GetAllAsync();
    Task<List<ContentBundle>> GetByAgeGroupAsync(string ageGroup);
}
