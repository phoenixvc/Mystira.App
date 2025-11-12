using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using CharacterMetadata = Mystira.App.Admin.Api.Models.CharacterMetadata;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize] // Requires admin authentication
public class AdminController : Controller
{
    private readonly IScenarioApiService _scenarioService;
    private readonly ICharacterMapApiService _characterMapService;
    private readonly IAppStatusService _appStatusService;
    private readonly IBundleService _bundleService;
    private readonly ICharacterMapFileService _characterMapFileService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ICharacterMediaMetadataService _characterMediaMetadataService;
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IScenarioApiService scenarioService,
        ICharacterMapApiService characterMapService,
        IAppStatusService appStatusService,
        IBundleService bundleService,
        ICharacterMapFileService characterMapFileService,
        IMediaMetadataService mediaMetadataService,
        ICharacterMediaMetadataService characterMediaMetadataService,
        MystiraAppDbContext context,
        ILogger<AdminController> logger)
    {
        _scenarioService = scenarioService;
        _characterMapService = characterMapService;
        _appStatusService = appStatusService;
        _bundleService = bundleService;
        _characterMapFileService = characterMapFileService;
        _mediaMetadataService = mediaMetadataService;
        _characterMediaMetadataService = characterMediaMetadataService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Login page for admin access
    /// </summary>
    [AllowAnonymous] // Allow anonymous access to login page
    [HttpGet("login")]
    public IActionResult Login()
    {
        // If already authenticated, redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard");
        }
        
        return View("Login");
    }

    /// <summary>
    /// Admin dashboard - serves the content management interface
    /// </summary>
    [HttpGet]
    public IActionResult Dashboard()
    {
        return View("Dashboard");
    }

    /// <summary>
    /// Scenarios management page
    /// </summary>
    [HttpGet("scenarios")]
    public IActionResult Scenarios()
    {
        return View("Scenarios");
    }

    /// <summary>
    /// Media management page
    /// </summary>
    [HttpGet("media")]
    public IActionResult Media()
    {
        return View("Media");
    }

    /// <summary>
    /// Media metadata management page
    /// </summary>
    [HttpGet("media-metadata")]
    public IActionResult MediaMetadata()
    {
        return View("MediaMetadata");
    }

    /// <summary>
    /// Character media metadata management page
    /// </summary>
    [HttpGet("character-media-metadata")]
    public IActionResult CharacterMediaMetadata()
    {
        return View("CharacterMediaMetadata");
    }

    /// <summary>
    /// Create new scenario page
    /// </summary>
    [HttpGet("scenarios/import")]
    public IActionResult ImportScenario()
    {
        return View("ImportScenario");
    }

    /// <summary>
    /// Edit existing scenario page
    /// </summary>
    [HttpGet("scenarios/edit/{id}")]
    public async Task<IActionResult> EditScenario(string id)
    {
        try
        {
            var scenario = await _scenarioService.GetScenarioByIdAsync(id);
            if (scenario == null)
            {
                return NotFound();
            }
            return View("EditScenario", scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scenario for editing: {ScenarioId}", id);
            return StatusCode(500, "Error loading scenario");
        }
    }

    /// <summary>
    /// Import media page
    /// </summary>
    [HttpGet("media/import")]
    public IActionResult ImportMedia()
    {
        return View("ImportMedia");
    }

    /// <summary>
    /// Import bundle page
    /// </summary>
    [HttpGet("bundles/import")]
    public IActionResult ImportBundle()
    {
        return View("ImportBundle");
    }

    /// <summary>
    /// Import badges page
    /// </summary>
    [HttpGet("badges/import")]
    public IActionResult ImportBadges()
    {
        return View("ImportBadges");
    }

    /// <summary>
    /// Character Maps management page
    /// </summary>
    [HttpGet("charactermaps")]
    public IActionResult CharacterMaps()
    {
        return View("CharacterMaps");
    }

    /// <summary>
    /// Import character map page
    /// </summary>
    [HttpGet("charactermaps/import")]
    public IActionResult ImportCharacterMap()
    {
        return View("ImportCharacterMap");
    }

    /// <summary>
    /// Edit existing character map page
    /// </summary>
    [HttpGet("charactermaps/edit/{id}")]
    public async Task<IActionResult> EditCharacterMap(string id)
    {
        try
        {
            var characterMap = await _characterMapService.GetCharacterMapAsync(id);
            if (characterMap == null)
            {
                return NotFound();
            }
            return View("EditCharacterMap", characterMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading character map for editing: {CharacterMapId}", id);
            return StatusCode(500, "Error loading character map");
        }
    }

    /// <summary>
    /// App status configuration page
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> AppStatus()
    {
        try
        {
            var appStatus = await _appStatusService.GetAppStatusAsync();
            return View("AppStatus", appStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading app status");
            return StatusCode(500, "Error loading app status");
        }
    }

    /// <summary>
    /// Update app status configuration
    /// </summary>
    [HttpPost("status")]
    public async Task<IActionResult> UpdateAppStatus([FromForm] AppStatusConfiguration config)
    {
        try
        {
            // Ensure nullable strings are never null
            config.MaintenanceMessage ??= string.Empty;
            config.UpdateMessage ??= string.Empty;
            
            await _appStatusService.UpdateAppStatusAsync(config);
            TempData["SuccessMessage"] = "App status configuration updated successfully.";
            return RedirectToAction("AppStatus");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating app status configuration");
            TempData["ErrorMessage"] = "Failed to update app status configuration.";
            return RedirectToAction("AppStatus");
        }
    }

    /// <summary>
    /// Handle character map YAML file upload
    /// </summary>
    [HttpPost("charactermaps/import")]
    public async Task<IActionResult> ImportCharacterMapYaml([FromForm] IFormFile yamlFile, [FromForm] string? name = null, [FromForm] bool validateReferences = true, [FromForm] bool overwriteExisting = false)
    {
        try
        {
            if (yamlFile == null || yamlFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "No YAML file provided" });
            }

            if (!yamlFile.FileName.EndsWith(".yaml") && !yamlFile.FileName.EndsWith(".yml"))
            {
                return BadRequest(new { success = false, message = "Please upload a .yaml or .yml file" });
            }

            // For now, return success - implement YAML parsing in the character map service
            using var stream = yamlFile.OpenReadStream();
            var characterMaps = await _characterMapService.ImportCharacterMapsFromYamlAsync(stream);
            
            return Ok(new { success = true, message = $"Successfully imported {characterMaps.Count} character map(s)", count = characterMaps.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character map YAML: {FileName}", yamlFile?.FileName);
            return BadRequest(new { success = false, message = $"Error importing character map: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validates a bundle file
    /// </summary>
    [HttpPost("bundles/validate")]
    public async Task<IActionResult> ValidateBundle([FromForm] IFormFile bundleFile)
    {
        try
        {
            if (bundleFile == null || bundleFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "No bundle file provided" });
            }

            var result = await _bundleService.ValidateBundleAsync(bundleFile);
            return Ok(new { success = result.IsValid, result = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bundle file: {FileName}", bundleFile?.FileName);
            return BadRequest(new { success = false, message = $"Error validating bundle: {ex.Message}" });
        }
    }

    /// <summary>
    /// Uploads and processes a bundle file
    /// </summary>
    [HttpPost("bundles/upload")]
    public async Task<IActionResult> UploadBundle([FromForm] IFormFile bundleFile, [FromForm] bool validateReferences = true, [FromForm] bool overwriteExisting = false)
    {
        try
        {
            if (bundleFile == null || bundleFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "No bundle file provided" });
            }

            var request = new BundleUploadRequest
            {
                ValidateReferences = validateReferences,
                OverwriteExisting = overwriteExisting
            };

            var result = await _bundleService.UploadBundleAsync(bundleFile, request);
            return Ok(new { success = result.Success, result = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading bundle file: {FileName}", bundleFile?.FileName);
            return BadRequest(new { success = false, message = $"Error uploading bundle: {ex.Message}" });
        }
    }

    /// <summary>
    /// Initialize sample data for development/testing
    /// </summary>
    [HttpPost("initialize-sample-data")]
    public async Task<IActionResult> InitializeSampleData()
    {
        try
        {
            // Initialize sample characters
            await InitializeSampleCharacters();
            
            // Initialize sample media metadata
            await InitializeSampleMediaMetadata();
            
            // Initialize sample character media metadata
            await InitializeSampleCharacterMediaMetadata();
            
            return Ok(new { success = true, message = "Sample data initialized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sample data");
            return BadRequest(new { success = false, message = $"Error initializing sample data: {ex.Message}" });
        }
    }

    /// <summary>
    /// Fix metadata JSON format issues
    /// </summary>
    [HttpPost("fix-metadata-format")]
    public async Task<IActionResult> FixMetadataFormat()
    {
        try
        {
            // Clear existing metadata files that might have format issues
            var existingMediaMetadata = await _context.MediaMetadataFiles.ToListAsync();
            if (existingMediaMetadata.Any())
            {
                _context.MediaMetadataFiles.RemoveRange(existingMediaMetadata);
            }

            var existingCharacterMediaMetadata = await _context.CharacterMediaMetadataFiles.ToListAsync();
            if (existingCharacterMediaMetadata.Any())
            {
                _context.CharacterMediaMetadataFiles.RemoveRange(existingCharacterMediaMetadata);
            }

            // Create fresh metadata files
            var mediaMetadataFile = new MediaMetadataFile
            {
                Id = "media-metadata",
                Entries = new List<MediaMetadataEntry>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };

            var characterMediaMetadataFile = new CharacterMediaMetadataFile
            {
                Id = "character-media-metadata",
                Entries = new List<CharacterMediaMetadataEntry>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = "1.0"
            };

            await _context.MediaMetadataFiles.AddAsync(mediaMetadataFile);
            await _context.CharacterMediaMetadataFiles.AddAsync(characterMediaMetadataFile);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Metadata format fixed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing metadata format");
            return BadRequest(new { success = false, message = $"Error fixing metadata format: {ex.Message}" });
        }
    }

    private async Task InitializeSampleCharacters()
    {
        // Check if characters already exist
        var existingCharacterMap = await _characterMapFileService.GetCharacterMapFileAsync();
        if (existingCharacterMap.Characters.Any())
        {
            return; // Characters already exist
        }

        // Add sample characters
        existingCharacterMap.Characters = new List<Character>
        {
            new Character
            {
                Id = "bear-maple-younger-kids",
                Name = "Maple",
                Image = "image-bear-maple-younger-kids--ede14750",
                Metadata = new CharacterMetadata
                {
                    Roles = new List<string> { "Peacemaker", "Emotion Mender", "The Bridge" },
                    Archetypes = new List<string> { "The Heart Warmer", "The Comfort Giver", "The Listener" },
                    Species = "bear",
                    Age = 8,
                    Traits = new List<string> { "kind-hearted", "patient", "gentle", "responsible", "fair-minded" },
                    Backstory = "Maple is a young bear whose quiet strength comes not from his size, but from his immense patience and kindness."
                }
            },
            new Character
            {
                Id = "fox-jinx-younger-kids",
                Name = "Jinx",
                Image = "image-fox-jinx-younger-kids-bf9d103d",
                Metadata = new CharacterMetadata
                {
                    Roles = new List<string> { "Bold Striker", "Risk Taker", "The First to Try" },
                    Archetypes = new List<string> { "The Brave Buddy", "The Explorer", "The Trickster" },
                    Species = "fox",
                    Age = 7,
                    Traits = new List<string> { "brave", "impulsive", "energetic", "confident", "loyal" },
                    Backstory = "Jinx is a bold young fox with clever eyes and a tail that's often twitching with excitement."
                }
            }
        };

        await _characterMapFileService.UpdateCharacterMapFileAsync(existingCharacterMap);
        _logger.LogInformation("Sample characters initialized successfully");
    }

    private async Task InitializeSampleMediaMetadata()
    {
        var sampleEntries = new List<MediaMetadataEntry>
        {
            new MediaMetadataEntry
            {
                Id = "image-1-a-frightened-friend-2-noticing-the-berries-93b7262c",
                Title = "1. A Frightened Friend, 2. Noticing the Berries",
                FileName = "1. A Frightened Friend, 2. Noticing the Berries.png",
                Type = "image",
                Description = "A serene, magical forest is illuminated by sunbeams filtering through a dense canopy of trees. In a grassy clearing by a gentle stream, a pile of luminous golden berries rests on a mossy mound, glowing with a warm, enchanting light. The air is filled with sparkling motes of light, adding to the whimsical and peaceful atmosphere of the scene",
                AgeRating = 1,
                Loopable = false
            },
            new MediaMetadataEntry
            {
                Id = "image-a-cheerful-visit-10bb4a77",
                Title = "A Cheerful Visit",
                FileName = "A Cheerful Visit.png",
                Type = "image",
                Description = "A cute mouse with large, friendly eyes sits cozily on a red knitted blanket at the entrance of its burrow. The burrow, nestled amongst roots and leaves, is warmly illuminated by strings of glowing lights, creating a safe and inviting atmosphere",
                SubjectReferenceId = "cute_mouse",
                AgeRating = 1,
                Loopable = false
            },
            // Sample scenario media files
            new MediaMetadataEntry
            {
                Id = "sea-monster-pearl",
                Title = "Sea Monster Pearl",
                FileName = "sea_monster_pearl.jpg",
                Type = "image",
                Description = "An ancient sea monster guarding a glowing pearl on the ocean floor",
                AgeRating = 1,
                Loopable = false
            },
            new MediaMetadataEntry
            {
                Id = "deep-ocean-sounds",
                Title = "Deep Ocean Sounds",
                FileName = "deep_ocean_sounds.mp3",
                Type = "audio",
                Description = "Ambient sounds of the deep ocean with mysterious underwater echoes",
                AgeRating = 1,
                Loopable = true
            },
            new MediaMetadataEntry
            {
                Id = "dragon-hoard-treasure",
                Title = "Dragon Hoard Treasure",
                FileName = "dragon_hoard_treasure.jpg",
                Type = "image",
                Description = "A massive dragon sitting atop a pile of golden treasure and jewels",
                AgeRating = 1,
                Loopable = false
            },
            new MediaMetadataEntry
            {
                Id = "cave-ambience",
                Title = "Cave Ambience",
                FileName = "cave_ambience.mp3",
                Type = "audio",
                Description = "Echoing cave sounds with distant dripping water",
                AgeRating = 1,
                Loopable = true
            },
            new MediaMetadataEntry
            {
                Id = "magical-forest",
                Title = "Magical Forest",
                FileName = "magical_forest.jpg",
                Type = "image",
                Description = "An enchanted forest with glowing trees and mystical creatures",
                AgeRating = 1,
                Loopable = false
            },
            new MediaMetadataEntry
            {
                Id = "forest-sounds",
                Title = "Forest Sounds",
                FileName = "forest_sounds.mp3",
                Type = "audio",
                Description = "Peaceful forest ambience with birds chirping and leaves rustling",
                AgeRating = 1,
                Loopable = true
            }
        };

        await _mediaMetadataService.ImportMediaMetadataEntriesAsync(
            System.Text.Json.JsonSerializer.Serialize(sampleEntries), true);
    }

    private async Task InitializeSampleCharacterMediaMetadata()
    {
        var sampleEntries = new List<CharacterMediaMetadataEntry>
        {
            new CharacterMediaMetadataEntry
            {
                Id = "image-bear-maple-younger-kids--ede14750",
                Title = "Bear Maple Younger Kids ",
                FileName = "Bear Maple Younger Kids .png",
                Type = "image",
                Description = "A full-body digital illustration of a friendly, young cartoon bear in a stylized 3D render style. The character has soft brown fur, a lighter tan-colored chest and muzzle, large expressive eyes, and a gentle smile. It is standing in a neutral, forward-facing pose against a plain, light grey background with a soft drop shadow",
                AgeRating = "E",
                Tags = ["bear", "nature", "animal", "cartoon bear", "character", "3D render", "digital illustration", "animation", "storybook", "cute", "whimsical", "brown fur", "tan", "big eyes", "furry", "smiling", "full body", "standing", "front view", "isolated character", "drop shadow", "friendly", "happy", "adorable", "gentle", "innocent", "character design"],
                Loopable = false
            },
            new CharacterMediaMetadataEntry
            {
                Id = "image-fox-jinx-younger-kids-bf9d103d",
                Title = "Fox Jinx Younger kids",
                FileName = "Fox Jinx Younger kids.png",
                Type = "image",
                Description = "A full-body digital illustration of an adorable, young cartoon fox in a stylized 3D render style. The fox has bright orange fur with white accents on its chest, muzzle, and the tip of its bushy tail. It features large, expressive green eyes and a confident, friendly smile. The character is in a sitting pose at a three-quarter view, looking towards the camera against a plain, light grey background with a soft drop shadow",
                AgeRating = "E",
                Tags = ["fox", "animal", "cartoon fox", "nature", "character", "3D render", "digital illustration", "animation", "storybook", "cute", "whimsical", "orange fur", "white", "green eyes", "bushy tail", "smiling", "full body", "sitting", "three-quarter view", "isolated character", "drop shadow", "friendly", "happy", "adorable", "clever", "confident", "brave", "character design"],
                Loopable = false
            }
        };

        await _characterMediaMetadataService.ImportCharacterMediaMetadataEntriesAsync(
            System.Text.Json.JsonSerializer.Serialize(sampleEntries), true);
    }

    /// <summary>
    /// Upload a scenario file for import
    /// </summary>
    [HttpPost("scenarios/upload")]
    public async Task<IActionResult> UploadScenario([FromForm] IFormFile scenarioFile, [FromForm] bool overwriteExisting = false)
    {
        try
        {
            if (scenarioFile == null || scenarioFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "No scenario file provided" });
            }

            // Read the YAML content
            using var stream = scenarioFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var yamlContent = await reader.ReadToEndAsync();

            // Parse and create the scenario
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            var scenarioData = (Dictionary<object, object>) deserializer.Deserialize<dynamic>(yamlContent);
            var createRequest = ScenarioRequestCreator.Create(scenarioData);

            // Check for existing scenario with same title
            var existingScenarios = await _scenarioService.GetScenariosAsync(new ScenarioQueryRequest { PageSize = 1000 });
            var existingScenario = existingScenarios.Scenarios.FirstOrDefault(s => 
                s.Title.Equals(createRequest.Title, StringComparison.OrdinalIgnoreCase));

            Scenario scenario;
            if (existingScenario != null && !overwriteExisting)
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Scenario with title '{createRequest.Title}' already exists. Set overwriteExisting=true to update it.",
                    existingScenarioId = existingScenario.Id
                });
            }
            else if (existingScenario != null && overwriteExisting)
            {
                // Update existing scenario
                scenario = await _scenarioService.UpdateScenarioAsync(existingScenario.Id, createRequest);
                if (scenario == null)
                {
                    return BadRequest(new { success = false, message = "Failed to update existing scenario" });
                }
            }
            else
            {
                // Create new scenario
                scenario = await _scenarioService.CreateScenarioAsync(createRequest);
            }

            return Ok(new { 
                success = true, 
                message = "Scenario uploaded successfully", 
                scenarioId = scenario.Id,
                scenarioTitle = scenario.Title 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading scenario file: {FileName}", scenarioFile?.FileName);
            return BadRequest(new { success = false, message = $"Error uploading scenario: {ex.Message}" });
        }
    }
}