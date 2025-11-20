using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Data;
using Mystira.App.Api.Models;
using Mystira.App.Admin.Api.Validation;
using NJsonSchema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mystira.App.Api.Services;

public class ScenarioApiService : IScenarioApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<ScenarioApiService> _logger;
    private readonly IMediaApiService _mediaService;
    private readonly ICharacterMapFileService _characterService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ICharacterMediaMetadataService _characterMetadataService;

    private static readonly JsonSchema ScenarioJsonSchema = JsonSchema.FromJsonAsync(ScenarioSchemaDefinitions.StorySchema).GetAwaiter().GetResult();

    private static readonly JsonSerializerOptions SchemaSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ScenarioApiService(
        MystiraAppDbContext context,
        ILogger<ScenarioApiService> logger,
        IMediaApiService mediaService,
        ICharacterMapFileService characterService,
        IMediaMetadataService mediaMetadataService,
        ICharacterMediaMetadataService characterMetadataService)
    {
        _context = context;
        _logger = logger;
        _mediaService = mediaService;
        _characterService = characterService;
        _mediaMetadataService = mediaMetadataService;
        _characterMetadataService = characterMetadataService;
    }

    public async Task<ScenarioListResponse> GetScenariosAsync(ScenarioQueryRequest request)
    {
        var query = _context.Scenarios.AsQueryable();

        if (request.Difficulty.HasValue)
        {
            query = query.Where(s => s.Difficulty == request.Difficulty.Value);
        }

        if (request.SessionLength.HasValue)
        {
            query = query.Where(s => s.SessionLength == request.SessionLength.Value);
        }

        if (request.MinimumAge.HasValue)
        {
            query = query.Where(s => s.MinimumAge <= request.MinimumAge.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AgeGroup))
        {
            var targetMinimumAge = GetMinimumAgeForGroup(request.AgeGroup);
            if (targetMinimumAge.HasValue)
            {
                query = query.Where(s => s.MinimumAge <= targetMinimumAge.Value);
            }
            else
            {
                query = query.Where(s => s.AgeGroup == request.AgeGroup);
            }
        }

        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                query = query.Where(s => s.Tags.Contains(tag));
            }
        }

        if (request.Archetypes?.Any() == true)
        {
            foreach (var archetype in request.Archetypes)
            {
                query = query.Where(s => s.Archetypes.Select(a => a.Value).Contains(archetype));
            }
        }

        if (request.CoreAxes?.Any() == true)
        {
            foreach (var axis in request.CoreAxes)
            {
                query = query.Where(s => s.CoreAxes.Select(a => a.Value).Contains(axis));
            }
        }

        var totalCount = await query.CountAsync();

        var scenarios = await query
            .OrderBy(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ScenarioSummary
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Tags = s.Tags,
                Difficulty = s.Difficulty,
                SessionLength = s.SessionLength,
                Archetypes = s.Archetypes.Select(a => a.Value).ToList(),
                MinimumAge = s.MinimumAge,
                AgeGroup = s.AgeGroup,
                CoreAxes = s.CoreAxes.Select(a => a.Value).ToList(),
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return new ScenarioListResponse
        {
            Scenarios = scenarios,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            HasNextPage = (request.Page * request.PageSize) < totalCount
        };
    }

    public async Task<Scenario?> GetScenarioByIdAsync(string id)
    {
        return await _context.Scenarios
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Scenario> CreateScenarioAsync(CreateScenarioRequest request)
    {
        ValidateAgainstSchema(request);

        var scenario = new Scenario
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
            Difficulty = request.Difficulty,
            SessionLength = request.SessionLength,
            Archetypes = request.Archetypes.Select(a => Archetype.Parse(a)!).ToList(),
            AgeGroup = request.AgeGroup,
            MinimumAge = request.MinimumAge,
            CoreAxes = request.CoreAxes.Select(a => CoreAxis.Parse(a)!).ToList(),
            Characters = request.Characters,
            Scenes = request.Scenes,
            CreatedAt = DateTime.UtcNow
        };

        await ValidateScenarioAsync(scenario);

        _context.Scenarios.Add(scenario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving scenario: {ScenarioId}", scenario.Id);
            throw;
        }

        _logger.LogInformation("Created new scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }

    public async Task<Scenario?> UpdateScenarioAsync(string id, CreateScenarioRequest request)
    {
        var scenario = await _context.Scenarios.FirstOrDefaultAsync(s => s.Id == id);
        if (scenario == null)
            return null;

        ValidateAgainstSchema(request);

        scenario.Title = request.Title;
        scenario.Description = request.Description;
        scenario.Tags = request.Tags;
        scenario.Difficulty = request.Difficulty;
        scenario.SessionLength = request.SessionLength;
        scenario.Archetypes = request.Archetypes.Select(a => Archetype.Parse(a)!).ToList();
        scenario.AgeGroup = request.AgeGroup;
        scenario.MinimumAge = request.MinimumAge;
        scenario.CoreAxes = request.CoreAxes.Select(a => CoreAxis.Parse(a)!).ToList();
        scenario.Characters = request.Characters;
        scenario.Scenes = request.Scenes;

        await ValidateScenarioAsync(scenario);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }

    public async Task<bool> DeleteScenarioAsync(string id)
    {
        var scenario = await _context.Scenarios.FirstOrDefaultAsync(s => s.Id == id);
        if (scenario == null)
            return false;

        _context.Scenarios.Remove(scenario);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return true;
    }

    public async Task<List<Scenario>> GetScenariosByAgeGroupAsync(string ageGroup)
    {
        var scenarios = await _context.Scenarios.ToListAsync();

        if (string.IsNullOrWhiteSpace(ageGroup))
        {
            return scenarios
                .OrderBy(s => s.Title)
                .ToList();
        }

        var targetMinimumAge = GetMinimumAgeForGroup(ageGroup);
        if (targetMinimumAge.HasValue)
        {
            return scenarios
                .Where(s => s.MinimumAge <= targetMinimumAge.Value)
                .OrderBy(s => s.Title)
                .ToList();
        }

        return scenarios
            .Where(s => string.Equals(s.AgeGroup, ageGroup, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Title)
            .ToList();
    }

    private static int? GetMinimumAgeForGroup(string ageGroup)
    {
        if (string.IsNullOrWhiteSpace(ageGroup))
        {
            return null;
        }

        var knownGroup = AgeGroup.Parse(ageGroup);
        if (knownGroup != null)
        {
            return knownGroup.MinimumAge;
        }

        if (TryParseAgeRangeMinimum(ageGroup, out var parsedMinimum))
        {
            return parsedMinimum;
        }

        return null;
    }

    private static bool TryParseAgeRangeMinimum(string value, out int minimumAge)
    {
        minimumAge = 0;
        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out var min))
        {
            minimumAge = min;
            return true;
        }

        return false;
    }

    private void ValidateAgainstSchema(CreateScenarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tags = request.Tags ?? new List<string>();
        var coreAxes = request.CoreAxes ?? new List<string>();
        var archetypes = request.Archetypes ?? new List<string>();
        var characters = request.Characters ?? new List<ScenarioCharacter>();
        var scenes = request.Scenes ?? new List<Scene>();

        var payload = new
        {
            Title = request.Title,
            Description = request.Description,
            Tags = tags,
            Difficulty = request.Difficulty.ToString(),
            SessionLength = request.SessionLength.ToString(),
            AgeGroup = request.AgeGroup,
            MinimumAge = request.MinimumAge,
            CoreAxes = coreAxes,
            Archetypes = archetypes,
            Characters = characters.Select(character => new
            {
                Id = character.Id,
                Name = character.Name,
                Image = character.Image,
                Audio = character.Audio,
                Metadata = character.Metadata == null ? null : new
                {
                    Role = character.Metadata.Role ?? new List<string>(),
                    Archetype = character.Metadata.Archetype?.Select(a => a.Value).ToList() ?? new List<string>(),
                    Species = character.Metadata.Species,
                    Age = character.Metadata.Age,
                    Traits = character.Metadata.Traits ?? new List<string>(),
                    Backstory = character.Metadata.Backstory
                }
            }).ToList(),
            Scenes = scenes.Select(scene =>
            {
                var media = scene.Media;
                var hasMedia = media != null && (!string.IsNullOrWhiteSpace(media.Image) ||
                                                 !string.IsNullOrWhiteSpace(media.Audio) ||
                                                 !string.IsNullOrWhiteSpace(media.Video));

                var branches = scene.Branches ?? new List<Branch>();
                var echoReveals = scene.EchoReveals ?? new List<EchoReveal>();

                return new
                {
                    Id = scene.Id,
                    Title = scene.Title,
                    Type = scene.Type.ToString().ToLowerInvariant(),
                    Description = scene.Description,
                    NextScene = string.IsNullOrWhiteSpace(scene.NextSceneId) ? null : scene.NextSceneId,
                    Difficulty = scene.Difficulty,
                    Media = hasMedia ? new
                    {
                        Image = media?.Image,
                        Audio = media?.Audio,
                        Video = media?.Video
                    } : null,
                    Branches = branches.Select(branch => new
                    {
                        Choice = branch.Choice,
                        NextScene = string.IsNullOrWhiteSpace(branch.NextSceneId) ? null : branch.NextSceneId,
                        EchoLog = branch.EchoLog == null ? null : new
                        {
                            EchoType = branch.EchoLog.EchoType,
                            Description = branch.EchoLog.Description,
                            Strength = branch.EchoLog.Strength
                        },
                        CompassChange = branch.CompassChange == null ? null : new
                        {
                            Axis = branch.CompassChange.Axis,
                            Delta = branch.CompassChange.Delta,
                            DevelopmentalLink = branch.CompassChange.DevelopmentalLink
                        }
                    }).ToList(),
                    EchoReveals = echoReveals.Select(reveal => new
                    {
                        EchoType = reveal.EchoType,
                        MinStrength = reveal.MinStrength,
                        TriggerSceneId = reveal.TriggerSceneId,
                        MaxAgeScenes = reveal.MaxAgeScenes,
                        RevealMechanic = reveal.RevealMechanic,
                        Required = reveal.Required
                    }).ToList()
                };
            }).ToList()
        };

        var serialized = JsonSerializer.Serialize(payload, SchemaSerializerOptions);
        var errors = ScenarioJsonSchema.Validate(serialized);

        if (errors.Count > 0)
        {
            var details = string.Join("; ", errors.Select(e => e.ToString()));
            throw new ScenarioValidationException($"Scenario document does not match the required schema: {details}");
        }
    }

    public async Task<List<Scenario>> GetFeaturedScenariosAsync()
    {
        // Return a curated list of featured scenarios
        return await _context.Scenarios
            .OrderBy(s => s.CreatedAt)
            .Take(6)
            .ToListAsync();
    }

    public async Task ValidateScenarioAsync(Scenario scenario)
    {
        try
        {
            // Validate basic scenario structure
            if (string.IsNullOrWhiteSpace(scenario.Title))
                throw new ScenarioValidationException("Scenario title cannot be empty");

            if (string.IsNullOrWhiteSpace(scenario.Description))
                throw new ScenarioValidationException("Scenario description cannot be empty");

            if (!scenario.Scenes.Any())
                throw new ScenarioValidationException("Scenario must contain at least one scene");

            // Validate scene structure
            foreach (var scene in scenario.Scenes)
            {
                if (string.IsNullOrWhiteSpace(scene.Id))
                    throw new ScenarioValidationException($"Scene is missing an ID (Title: {scene.Title})");

                if (string.IsNullOrWhiteSpace(scene.Title))
                    throw new ScenarioValidationException($"Scene is missing a title (ID: {scene.Id})");

                // Only choice scenes can have echo logs
                if (scene.Type != SceneType.Choice && scene.Branches.Any(b => b.EchoLog != null))
                    throw new ScenarioValidationException($"Only choice scenes can have echo logs (Scene ID: {scene.Id})");

                // Validate echo log values
                foreach (var branch in scene.Branches.Where(b => b.EchoLog != null))
                {
                    var echo = branch.EchoLog!;
                    if (echo.Strength < 0.1 || echo.Strength > 1.0)
                        throw new ScenarioValidationException($"Echo log strength must be between 0.1 and 1.0 (Scene ID: {scene.Id}, Choice: {branch.Choice})");

                    if (EchoType.Parse(echo.EchoType.Value) == null)
                        throw new ScenarioValidationException($"Invalid echo type '{echo.EchoType}' (Scene ID: {scene.Id}, Choice: {branch.Choice})");

                    if (string.IsNullOrWhiteSpace(echo.Description))
                        throw new ScenarioValidationException($"Echo log description cannot be empty (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                }

                // Validate compass changes
                foreach (var branch in scene.Branches.Where(b => b.CompassChange != null))
                {
                    var change = branch.CompassChange!;
                    if (change.Delta < -1.0 || change.Delta > 1.0)
                        throw new ScenarioValidationException($"Compass change delta must be between -1.0 and 1.0 (Scene ID: {scene.Id}, Choice: {branch.Choice})");

                    if (string.IsNullOrWhiteSpace(change.Axis))
                        throw new ScenarioValidationException($"Compass axis cannot be empty (Scene ID: {scene.Id}, Choice: {branch.Choice})");

                    if (!scenario.CoreAxes.Select(a => a.Value).Contains(change.Axis))
                    {
                        // TODO: re-enable strict validation when master axis list is finalized.
                        //throw new ScenarioValidationException($"Invalid compass axis '{change.Axis}' not defined in scenario (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }
                }

                // Validate branches have valid next scene IDs
                foreach (var branch in scene.Branches)
                {
                    // todo consider enforcing next scene ID is not END
                    // if (string.IsNullOrWhiteSpace(branch.NextSceneId))
                    //     throw new ScenarioValidationException($"Branch is missing next scene ID (Scene ID: {scene.Id}, Choice: {branch.Choice})");

                    if (branch.NextSceneId != "" && branch.NextSceneId != "END" && !scenario.Scenes.Any(s => s.Id == branch.NextSceneId))
                        throw new ScenarioValidationException($"Branch references non-existent next scene ID '{branch.NextSceneId}' (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                }
            }

            _logger.LogDebug("Scenario validation passed for: {ScenarioId}", scenario.Id);
        }
        catch (ScenarioValidationException)
        {
            // Re-throw validation exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scenario: {ScenarioId}", scenario.Id);
            throw new ScenarioValidationException($"Unexpected error validating scenario: {ex.Message}", ex);
        }
    }

    public async Task<ScenarioReferenceValidation> ValidateScenarioReferencesAsync(string scenarioId, bool includeMetadataValidation = true)
    {
        try
        {
            var scenario = await GetScenarioByIdAsync(scenarioId);
            if (scenario == null)
            {
                throw new ArgumentException($"Scenario not found: {scenarioId}");
            }

            var validation = new ScenarioReferenceValidation
            {
                ScenarioId = scenario.Id,
                ScenarioTitle = scenario.Title
            };

            // Get all media assets and character data
            var mediaQuery = new MediaQueryRequest { Page = 1, PageSize = 1000 };
            var mediaResponse = await _mediaService.GetMediaAsync(mediaQuery);
            var allMedia = mediaResponse.Media.ToDictionary(m => m.MediaId, m => m);

            var characterMapFile = await _characterService.GetCharacterMapFileAsync();
            var allCharacters = characterMapFile.Characters.ToDictionary(c => c.Id, c => c);

            MediaMetadataFile? mediaMetadata = null;
            CharacterMediaMetadataFile? characterMetadata = null;

            if (includeMetadataValidation)
            {
                try
                {
                    mediaMetadata = await _mediaMetadataService.GetMediaMetadataFileAsync();
                    characterMetadata = await _characterMetadataService.GetCharacterMediaMetadataFileAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load metadata files for scenario validation. Continuing without metadata validation.");
                    includeMetadataValidation = false; // Disable metadata validation if we can't load the files
                }
            }

            // Extract and validate references from all scenes
            foreach (var scene in scenario.Scenes)
            {
                await ValidateSceneReferences(scene, allMedia, allCharacters, mediaMetadata, characterMetadata, validation, includeMetadataValidation);
            }

            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scenario references: {ScenarioId}", scenarioId);
            throw;
        }
    }

    public async Task<List<ScenarioReferenceValidation>> ValidateAllScenarioReferencesAsync(bool includeMetadataValidation = true)
    {
        try
        {
            var query = new ScenarioQueryRequest { Page = 1, PageSize = 1000 };
            var response = await GetScenariosAsync(query);
            var results = new List<ScenarioReferenceValidation>();

            foreach (var scenarioSummary in response.Scenarios)
            {
                var validation = await ValidateScenarioReferencesAsync(scenarioSummary.Id, includeMetadataValidation);
                results.Add(validation);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating all scenario references");
            throw;
        }
    }

    private async Task ValidateSceneReferences(
        Scene scene,
        Dictionary<string, MediaAsset> allMedia,
        Dictionary<string, Character> allCharacters,
        MediaMetadataFile? mediaMetadata,
        CharacterMediaMetadataFile? characterMetadata,
        ScenarioReferenceValidation validation,
        bool includeMetadataValidation)
    {
        // Validate media references
        if (scene.Media != null)
        {
            await ValidateMediaReference(scene, scene.Media.Image, "image", allMedia, mediaMetadata, validation, includeMetadataValidation);
            await ValidateMediaReference(scene, scene.Media.Audio, "audio", allMedia, mediaMetadata, validation, includeMetadataValidation);
            await ValidateMediaReference(scene, scene.Media.Video, "video", allMedia, mediaMetadata, validation, includeMetadataValidation);
        }

        // Validate character references (from archetypes or other character-specific data)
        // For now, we'll check if any character IDs are mentioned in the scene description
        await ValidateCharacterReferences(scene, allCharacters, characterMetadata, validation, includeMetadataValidation);
    }

    private async Task ValidateMediaReference(
        Scene scene,
        string? mediaId,
        string mediaType,
        Dictionary<string, MediaAsset> allMedia,
        MediaMetadataFile? mediaMetadata,
        ScenarioReferenceValidation validation,
        bool includeMetadataValidation)
    {
        if (string.IsNullOrEmpty(mediaId)) return;

        var mediaExists = allMedia.ContainsKey(mediaId);
        var hasMetadata = includeMetadataValidation && mediaMetadata?.Entries.Any(e => e.Id == mediaId) == true;

        var mediaRef = new MediaReference
        {
            SceneId = scene.Id,
            SceneTitle = scene.Title,
            MediaId = mediaId,
            MediaType = mediaType,
            MediaExists = mediaExists,
            HasMetadata = hasMetadata || !includeMetadataValidation
        };

        validation.MediaReferences.Add(mediaRef);

        // Add missing reference if needed
        if (!mediaExists)
        {
            validation.MissingReferences.Add(new MissingReference
            {
                ReferenceId = mediaId,
                ReferenceType = "media",
                SceneId = scene.Id,
                SceneTitle = scene.Title,
                IssueType = "missing_file",
                Description = $"Media file '{mediaId}' ({mediaType}) not found in database"
            });
        }
        else if (includeMetadataValidation && !hasMetadata)
        {
            validation.MissingReferences.Add(new MissingReference
            {
                ReferenceId = mediaId,
                ReferenceType = "media",
                SceneId = scene.Id,
                SceneTitle = scene.Title,
                IssueType = "missing_metadata",
                Description = $"Media file '{mediaId}' ({mediaType}) exists but has no metadata"
            });
        }
    }

    private async Task ValidateCharacterReferences(
        Scene scene,
        Dictionary<string, Character> allCharacters,
        CharacterMediaMetadataFile? characterMetadata,
        ScenarioReferenceValidation validation,
        bool includeMetadataValidation)
    {
        // Look for character references in scene content
        // This is a simple implementation - could be enhanced to look for specific patterns
        var sceneContent = $"{scene.Title} {scene.Description}".ToLower();

        foreach (var character in allCharacters.Values)
        {
            var characterNameLower = character.Name.ToLower();

            // Check if character name appears in scene content
            if (sceneContent.Contains(characterNameLower))
            {
                var hasMetadata = includeMetadataValidation && characterMetadata?.Entries.Any(e => e.Id == character.Id) == true;

                var charRef = new CharacterReference
                {
                    SceneId = scene.Id,
                    SceneTitle = scene.Title,
                    CharacterId = character.Id,
                    CharacterName = character.Name,
                    CharacterExists = true, // Character exists if we found it
                    HasMetadata = hasMetadata || !includeMetadataValidation
                };

                validation.CharacterReferences.Add(charRef);

                // Add missing metadata reference if needed
                if (includeMetadataValidation && !hasMetadata)
                {
                    validation.MissingReferences.Add(new MissingReference
                    {
                        ReferenceId = character.Id,
                        ReferenceType = "character",
                        SceneId = scene.Id,
                        SceneTitle = scene.Title,
                        IssueType = "missing_metadata",
                        Description = $"Character '{character.Name}' is referenced but has no media metadata"
                    });
                }
            }
        }
    }

    // Define a custom exception for scenario validation errors
    public class ScenarioValidationException : Exception
    {
        public ScenarioValidationException(string message) : base(message) { }
        public ScenarioValidationException(string message, Exception innerException) : base(message, innerException) { }
    }

}