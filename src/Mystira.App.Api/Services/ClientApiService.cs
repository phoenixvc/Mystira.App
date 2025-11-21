using System.Text.RegularExpressions;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for handling client status and content synchronization operations
/// </summary>
public class ClientApiService : IClientApiService
{
    private readonly ILogger<ClientApiService> _logger;
    private readonly IScenarioApiService _scenarioService;
    private readonly IAppStatusService _appStatusService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientApiService"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="scenarioService">The scenario service</param>
    /// <param name="appStatusService">The app status service</param>
    public ClientApiService(
        ILogger<ClientApiService> logger,
        IScenarioApiService scenarioService,
        IAppStatusService appStatusService)
    {
        _logger = logger;
        _scenarioService = scenarioService;
        _appStatusService = appStatusService;
    }

    /// <inheritdoc />
    public async Task<ClientStatusResponse> GetClientStatusAsync(string clientVersion, string contentVersion)
    {
        try
        {
            _logger.LogInformation("Building client status: client_version={ClientVersion}, content_version={ContentVersion}",
                clientVersion, contentVersion);

            // Validate client version format (semver)
            if (!IsValidSemVer(clientVersion))
            {
                throw new ArgumentException("Invalid client_version format. Expected semver format (e.g., 1.2.3)");
            }

            // Get version information
            var versionInfo = await GetVersionInfoAsync();
            string minSupportedVersion = versionInfo.MinSupportedVersion;
            string latestVersion = versionInfo.LatestVersion;

            // Get current bundle version
            string currentBundleVersion = await GetCurrentBundleVersionAsync();

            // Check if client version is below minimum supported version
            bool updateRequired = await IsUpdateRequiredAsync(clientVersion);
            bool forceRefresh = updateRequired || string.IsNullOrEmpty(contentVersion);

            // Build content manifest
            var contentManifest = await BuildContentManifestAsync(contentVersion);

            // Check app status for force refresh
            var appStatus = await _appStatusService.GetAppStatusAsync();

            // Create response
            var response = new ClientStatusResponse
            {
                ForceRefresh = forceRefresh || appStatus.ForceContentRefresh,
                MinSupportedVersion = minSupportedVersion,
                LatestVersion = latestVersion,
                Message = GetStatusMessage(updateRequired, contentManifest),
                ContentManifest = contentManifest,
                BundleVersion = currentBundleVersion,
                UpdateRequired = updateRequired
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building client status");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ContentManifest> BuildContentManifestAsync(string clientContentVersion)
    {
        var manifest = new ContentManifest
        {
            Scenarios = new ScenarioChanges(),
            Media = new MediaChanges(),
            BundleVersion = await GetCurrentBundleVersionAsync()
        };

        try
        {
            // Parse client content version to determine what changes they need
            DateTime? clientVersionDate = ParseContentVersion(clientContentVersion);
            var currentVersionDate = DateTime.UtcNow;

            // If client has no version or version is different, determine changes
            if (clientVersionDate == null || clientContentVersion != manifest.BundleVersion)
            {
                // Query database for scenarios that have changed since client version
                var scenarioChanges = await GetScenarioChangesSinceAsync(clientVersionDate);
                manifest.Scenarios = scenarioChanges;

                // Query database for media changes since client version  
                var mediaChanges = await GetMediaChangesSinceAsync(clientVersionDate);
                manifest.Media = mediaChanges;

                _logger.LogInformation("Built content manifest with {AddedScenarios} added scenarios, {UpdatedScenarios} updated scenarios, {RemovedScenarios} removed scenarios",
                    manifest.Scenarios.Added?.Count ?? 0,
                    manifest.Scenarios.Updated?.Count ?? 0,
                    manifest.Scenarios.Removed?.Count ?? 0);
            }
            else
            {
                _logger.LogDebug("Client content version {ClientVersion} is up to date", clientContentVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building content manifest");
            // Return empty manifest on error
            manifest = new ContentManifest
            {
                Scenarios = new ScenarioChanges(),
                Media = new MediaChanges(),
                BundleVersion = await GetCurrentBundleVersionAsync()
            };
        }

        return manifest;
    }

    /// <inheritdoc />
    public async Task<bool> IsUpdateRequiredAsync(string clientVersion)
    {
        var versionInfo = await GetVersionInfoAsync();
        return IsVersionLowerThan(clientVersion, versionInfo.MinSupportedVersion);
    }

    /// <inheritdoc />
    public async Task<string> GetCurrentBundleVersionAsync()
    {
        return await _appStatusService.GetBundleVersionAsync();
    }

    /// <inheritdoc />
    public async Task<(string MinSupportedVersion, string LatestVersion)> GetVersionInfoAsync()
    {
        var minSupportedVersion = await _appStatusService.GetMinSupportedVersionAsync();
        var latestVersion = await _appStatusService.GetLatestVersionAsync();

        return (minSupportedVersion, latestVersion);
    }

    /// <summary>
    /// Validates if a version string follows semantic versioning format
    /// </summary>
    /// <param name="version">The version string to validate</param>
    /// <returns>True if the version is valid, false otherwise</returns>
    private bool IsValidSemVer(string version)
    {
        // Simple semver validation regex
        var semverRegex = new Regex(@"^\d+\.\d+\.\d+$");
        return semverRegex.IsMatch(version);
    }

    /// <summary>
    /// Checks if a version is lower than the minimum supported version
    /// </summary>
    /// <param name="version">The version to check</param>
    /// <param name="minVersion">The minimum supported version</param>
    /// <returns>True if the version is lower than the minimum, false otherwise</returns>
    private bool IsVersionLowerThan(string version, string minVersion)
    {
        try
        {
            var versionParts = version.Split('.').Select(int.Parse).ToArray();
            var minVersionParts = minVersion.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Min(versionParts.Length, minVersionParts.Length); i++)
            {
                if (versionParts[i] < minVersionParts[i])
                {
                    return true;
                }

                if (versionParts[i] > minVersionParts[i])
                {
                    return false;
                }
            }

            return versionParts.Length < minVersionParts.Length;
        }
        catch
        {
            // If parsing fails, assume version is lower
            return true;
        }
    }

    /// <summary>
    /// Gets an appropriate status message based on update requirements and content changes
    /// </summary>
    /// <param name="updateRequired">Whether an app update is required</param>
    /// <param name="manifest">The content manifest with changes</param>
    /// <returns>A user-friendly status message</returns>
    private string GetStatusMessage(bool updateRequired, ContentManifest manifest)
    {
        if (updateRequired)
        {
            return "Application update required. Please update to the latest version.";
        }

        bool hasScenarioChanges =
            (manifest.Scenarios?.Added?.Count > 0) ||
            (manifest.Scenarios?.Updated?.Count > 0) ||
            (manifest.Scenarios?.Removed?.Count > 0);

        bool hasMediaChanges =
            (manifest.Media?.Added?.Count > 0) ||
            (manifest.Media?.Updated?.Count > 0) ||
            (manifest.Media?.Removed?.Count > 0);

        if (hasScenarioChanges && hasMediaChanges)
        {
            return "New scenario and media assets available. Refresh recommended.";
        }
        else if (hasScenarioChanges)
        {
            return "New scenario content available. Refresh recommended.";
        }
        else if (hasMediaChanges)
        {
            return "New media assets available. Refresh recommended.";
        }

        return "Your content is up to date.";
    }

    /// <summary>
    /// Parses a content version string into a DateTime
    /// </summary>
    /// <param name="contentVersion">The content version string</param>
    /// <returns>The parsed DateTime or null if invalid</returns>
    private DateTime? ParseContentVersion(string contentVersion)
    {
        if (string.IsNullOrEmpty(contentVersion))
        {
            return null;
        }

        if (DateTime.TryParse(contentVersion, out DateTime result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets scenario changes since the specified date
    /// </summary>
    /// <param name="sinceDate">The date to check for changes since</param>
    /// <returns>Scenario changes since the date</returns>
    private async Task<ScenarioChanges> GetScenarioChangesSinceAsync(DateTime? sinceDate)
    {
        var changes = new ScenarioChanges
        {
            Added = new List<string>(),
            Updated = new List<string>(),
            Removed = new List<string>()
        };

        try
        {
            // Get all scenarios from the database
            var scenarioQuery = new ScenarioQueryRequest { PageSize = 1000 };
            var scenarios = await _scenarioService.GetScenariosAsync(scenarioQuery);

            if (sinceDate.HasValue)
            {
                // Find scenarios added since the client's content version
                var addedScenarios = scenarios.Scenarios.Where(s => s.CreatedAt > sinceDate.Value).ToList();
                changes.Added = addedScenarios.Select(s => s.Id).ToList();

                // For updated scenarios, we would need to track modification dates
                // For now, we'll use a simple approach - if we don't have a ModifiedAt field,
                // we can't determine updated scenarios accurately

                // In a real implementation, you would add a ModifiedAt field to Scenario
                // and query for scenarios where ModifiedAt > sinceDate && CreatedAt <= sinceDate

                _logger.LogDebug("Found {AddedScenarios} new scenarios since {SinceDate}",
                    changes.Added.Count, sinceDate.Value);
            }
            else
            {
                // Client has no content version, send all scenarios
                changes.Added = scenarios.Scenarios.Select(s => s.Id).ToList();
                _logger.LogDebug("Client has no content version, sending all {TotalScenarios} scenarios",
                    changes.Added.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scenario changes since {SinceDate}", sinceDate);
        }

        return changes;
    }

    /// <summary>
    /// Gets media changes since the specified date
    /// </summary>
    /// <param name="sinceDate">The date to check for changes since</param>
    /// <returns>Media changes since the date</returns>
    private async Task<MediaChanges> GetMediaChangesSinceAsync(DateTime? sinceDate)
    {
        var changes = new MediaChanges
        {
            Added = new List<MediaItem>(),
            Updated = new List<MediaItem>(),
            Removed = new List<string>()
        };

        try
        {
            // In a real implementation, you would query a media database table
            // For now, we'll return empty changes since we don't have a media service yet
            // TODO: Implement once media management is added

            _logger.LogDebug("Media changes query not yet implemented - returning empty changes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media changes since {SinceDate}", sinceDate);
        }

        return changes;
    }
}
