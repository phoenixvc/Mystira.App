using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IContentBundleAdminService
{
    Task<List<ContentBundle>> GetAllAsync();
    Task<ContentBundle?> GetByIdAsync(string id);
    Task<ContentBundle> CreateAsync(ContentBundle bundle);
    Task<ContentBundle?> UpdateAsync(string id, ContentBundle bundle);
    Task<bool> DeleteAsync(string id);
}
