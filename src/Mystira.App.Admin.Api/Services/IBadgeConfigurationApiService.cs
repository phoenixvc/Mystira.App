using Mystira.App.Domain.Models;
using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IBadgeConfigurationApiService
{
    Task<List<BadgeConfiguration>> GetAllBadgeConfigurationsAsync();
    Task<BadgeConfiguration?> GetBadgeConfigurationAsync(string id);
    Task<List<BadgeConfiguration>> GetBadgeConfigurationsByAxisAsync(string axis);
    Task<BadgeConfiguration> CreateBadgeConfigurationAsync(CreateBadgeConfigurationRequest request);
    Task<BadgeConfiguration?> UpdateBadgeConfigurationAsync(string id, UpdateBadgeConfigurationRequest request);
    Task<bool> DeleteBadgeConfigurationAsync(string id);
    Task<string> ExportBadgeConfigurationsAsYamlAsync();
    Task<List<BadgeConfiguration>> ImportBadgeConfigurationsFromYamlAsync(Stream yamlStream);
}