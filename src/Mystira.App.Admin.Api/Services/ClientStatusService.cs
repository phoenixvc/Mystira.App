using Mystira.App.Admin.Api.Models;
using ScenarioQueryRequest = Mystira.App.Contracts.Requests.Scenarios.ScenarioQueryRequest;

namespace Mystira.App.Admin.Api.Services;

public class ClientStatusService : IClientStatusService
{
    private readonly IScenarioApiService _scenarioService;
    private readonly ILogger<ClientStatusService> _logger;
    private readonly IConfiguration _configuration;

    public ClientStatusService(
        IScenarioApiService scenarioService,
        ILogger<ClientStatusService> logger,
        IConfiguration configuration)
    {
        _scenarioService = scenarioService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ClientStatusResponse> GetClientStatusAsync(ClientStatusRequest request)
    {
        try
        {
            var minSupportedVersion = _configuration["App:MinSupportedVersion"] ?? "1.0.0";
            var latestVersion = _configuration["App:LatestVersion"] ?? "1.0.0";
            var currentContentVersion = _configuration["App:ContentVersion"] ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Check if client version is supported
            var forceRefresh = false;
            var updateRequired = false;
            var message = "Your app is up to date.";

            if (!string.IsNullOrEmpty(request.ClientVersion))
            {
                var clientVersion = ParseVersion(request.ClientVersion);
                var minVersion = ParseVersion(minSupportedVersion);
                var latest = ParseVersion(latestVersion);

                if (clientVersion < minVersion)
                {
                    forceRefresh = true;
                    updateRequired = true;
                    message = $"Your app version ({request.ClientVersion}) is no longer supported. Please update to version {latestVersion} or later.";
                }
                else if (clientVersion < latest)
                {
                    updateRequired = true;
                    message = $"A new version ({latestVersion}) is available with improved features and scenarios.";
                }
            }

            // Generate content manifest
            var contentManifest = await GenerateContentManifestAsync(request.ContentVersion, currentContentVersion);

            // Check if content updates are available
            if (contentManifest.Scenarios.Added.Any() ||
                contentManifest.Scenarios.Updated.Any() ||
                contentManifest.Media.Added.Any() ||
                contentManifest.Media.Updated.Any())
            {
                updateRequired = true;
                if (message == "Your app is up to date.")
                {
                    message = "New scenario and media assets available. Refresh recommended.";
                }
            }

            return new ClientStatusResponse
            {
                ForceRefresh = forceRefresh,
                MinSupportedVersion = minSupportedVersion,
                LatestVersion = latestVersion,
                Message = message,
                ContentManifest = contentManifest,
                UpdateRequired = updateRequired
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client status");
            throw;
        }
    }

    private async Task<ContentManifest> GenerateContentManifestAsync(string? clientContentVersion, string currentContentVersion)
    {
        var manifest = new ContentManifest
        {
            BundleVersion = currentContentVersion
        };

        try
        {
            // For demo purposes, simulate some content changes
            // In a real implementation, this would check Azure storage for actual changes

            // Check if client has an older content version
            if (string.IsNullOrEmpty(clientContentVersion) ||
                ParseDateTime(clientContentVersion) < ParseDateTime(currentContentVersion))
            {
                // Get all scenarios to check for updates
                var scenarios = await _scenarioService.GetScenariosAsync(new ScenarioQueryRequest
                {
                    Page = 1,
                    PageSize = 1000 // Get all scenarios
                });

                // Simulate some scenarios being added/updated
                var recentScenarios = scenarios.Scenarios
                    .Where(s => s.CreatedAt > DateTime.UtcNow.AddDays(-30))
                    .Select(s => s.Id)
                    .ToList();

                manifest.Scenarios = new ScenarioChanges
                {
                    Added = recentScenarios.Take(2).ToList(),
                    Updated = recentScenarios.Skip(2).Take(1).ToList(),
                    Removed = new List<string>() // No removals for demo
                };

                // Simulate media updates
                manifest.Media = new MediaChanges
                {
                    Added = new List<MediaItem>
                    {
                        new()
                        {
                            MediaId = "stormy-sea-audio",
                            FilePath = "media/audio/stormy_sea.mp3",
                            Version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            Hash = GenerateHash("stormy-sea-audio")
                        }
                    },
                    Updated = new List<MediaItem>
                    {
                        new()
                        {
                            MediaId = "spooky-forest-ambience",
                            FilePath = "media/audio/spooky_forest.mp3",
                            Version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            Hash = GenerateHash("spooky-forest-ambience")
                        }
                    },
                    Removed = new List<string> { "old-dragon-roar", "outdated-tavern.mp4" }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating content manifest, returning empty manifest");
        }

        return manifest;
    }

    private static Version ParseVersion(string versionString)
    {
        try
        {
            return Version.Parse(versionString);
        }
        catch
        {
            return new Version(1, 0, 0);
        }
    }

    private static DateTime ParseDateTime(string dateTimeString)
    {
        try
        {
            return DateTime.Parse(dateTimeString);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static string GenerateHash(string input)
    {
        // Simple hash generation for demo - in production use proper hashing
        return Math.Abs(input.GetHashCode()).ToString("x8");
    }
}
