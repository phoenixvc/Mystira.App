using System.Text.Json;
using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service for managing application status configuration
/// </summary>
public class AppStatusService : IAppStatusService
{
    private readonly string _configFilePath;
    private readonly ILogger<AppStatusService> _logger;
    private AppStatusConfiguration? _cachedConfig;
    private DateTime _lastLoaded = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public AppStatusService(IWebHostEnvironment environment, ILogger<AppStatusService> logger)
    {
        _configFilePath = Path.Combine(environment.ContentRootPath, "appstatus.json");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppStatusConfiguration> GetAppStatusAsync()
    {
        if (_cachedConfig == null || DateTime.UtcNow - _lastLoaded > _cacheExpiry)
        {
            await LoadConfigurationAsync();
        }

        return _cachedConfig ?? new AppStatusConfiguration();
    }

    /// <inheritdoc />
    public async Task UpdateAppStatusAsync(AppStatusConfiguration config)
    {
        try
        {
            config.LastUpdated = DateTime.UtcNow;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_configFilePath, json);

            // Update cache
            _cachedConfig = config;
            _lastLoaded = DateTime.UtcNow;

            _logger.LogInformation("App status configuration updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update app status configuration");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetMinSupportedVersionAsync()
    {
        var config = await GetAppStatusAsync();
        return config.MinSupportedVersion;
    }

    /// <inheritdoc />
    public async Task<string> GetLatestVersionAsync()
    {
        var config = await GetAppStatusAsync();
        return config.LatestVersion;
    }

    /// <inheritdoc />
    public async Task<string> GetBundleVersionAsync()
    {
        var config = await GetAppStatusAsync();
        return config.BundleVersion;
    }

    /// <inheritdoc />
    public async Task UpdateBundleVersionAsync(string version)
    {
        var config = await GetAppStatusAsync();
        config.BundleVersion = version;
        config.ContentVersion = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        await UpdateAppStatusAsync(config);
    }

    /// <inheritdoc />
    public async Task<bool> IsMaintenanceModeAsync()
    {
        var config = await GetAppStatusAsync();
        return config.MaintenanceMode;
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogWarning("App status configuration file not found at {Path}. Creating default configuration.", _configFilePath);

                var defaultConfig = new AppStatusConfiguration();
                await UpdateAppStatusAsync(defaultConfig);
                return;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _cachedConfig = JsonSerializer.Deserialize<AppStatusConfiguration>(json, options);
            _lastLoaded = DateTime.UtcNow;

            _logger.LogDebug("App status configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load app status configuration. Using default configuration.");
            _cachedConfig = new AppStatusConfiguration();
            _lastLoaded = DateTime.UtcNow;
        }
    }
}
