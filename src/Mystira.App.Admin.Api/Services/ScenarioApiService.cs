using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Validation;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using NJsonSchema;
using CharacterMediaMetadataEntry = Mystira.App.Domain.Models.CharacterMediaMetadataEntry;
using CharacterMediaMetadataFile = Mystira.App.Domain.Models.CharacterMediaMetadataFile;
using CharacterMetadata = Mystira.App.Domain.Models.CharacterMetadata;
using CharacterReference = Mystira.App.Contracts.Responses.Scenarios.CharacterReference;
using ClassificationTag = Mystira.App.Domain.Models.ClassificationTag;
using CreateScenarioRequest = Mystira.App.Contracts.Requests.Scenarios.CreateScenarioRequest;
using MediaMetadataEntry = Mystira.App.Domain.Models.MediaMetadataEntry;
using MediaMetadataFile = Mystira.App.Domain.Models.MediaMetadataFile;
using MediaReference = Mystira.App.Contracts.Responses.Scenarios.MediaReference;
using MissingReference = Mystira.App.Contracts.Responses.Scenarios.MissingReference;
using Modifier = Mystira.App.Domain.Models.Modifier;
using ScenarioListResponse = Mystira.App.Contracts.Responses.Scenarios.ScenarioListResponse;
using ScenarioQueryRequest = Mystira.App.Contracts.Requests.Scenarios.ScenarioQueryRequest;
using ScenarioReferenceValidation = Mystira.App.Contracts.Responses.Scenarios.ScenarioReferenceValidation;
using ScenarioSummary = Mystira.App.Contracts.Responses.Scenarios.ScenarioSummary;

namespace Mystira.App.Admin.Api.Services;

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
        // Build server-translatable base query first (avoid value-object access in provider translation)
        var baseQuery = _context.Scenarios.AsQueryable();

        if (request.Difficulty.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.Difficulty == request.Difficulty.Value);
        }

        if (request.SessionLength.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.SessionLength == request.SessionLength.Value);
        }

        if (request.MinimumAge.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.MinimumAge <= request.MinimumAge.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AgeGroup))
        {
            var targetMinimumAge = GetMinimumAgeForGroup(request.AgeGroup);
            if (targetMinimumAge.HasValue)
            {
                baseQuery = baseQuery.Where(s => s.MinimumAge <= targetMinimumAge.Value);
            }
            else
            {
                baseQuery = baseQuery.Where(s => s.AgeGroup == request.AgeGroup);
            }
        }

        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                baseQuery = baseQuery.Where(s => s.Tags.Contains(tag));
            }
        }

        // Materialize after base filters to perform value-object filters and projections safely in-memory
        var prefiltered = await baseQuery.ToListAsync();

        // Apply Archetype/CoreAxis filters in-memory to avoid provider translation of a.Value
        IEnumerable<Scenario> filtered = prefiltered;

        if (request.Archetypes?.Any() == true)
        {
            foreach (var archetype in request.Archetypes)
            {
                filtered = filtered.Where(s => s.Archetypes != null && s.Archetypes.Any(a => a.Value == archetype));
            }
        }

        if (request.CoreAxes?.Any() == true)
        {
            foreach (var axis in request.CoreAxes)
            {
                filtered = filtered.Where(s => s.CoreAxes != null && s.CoreAxes.Any(a => a.Value == axis));
            }
        }

        var totalCount = filtered.Count();

        var scenarios = filtered
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
            .ToList();

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
            Archetypes = ParseArchetypesOrThrow(request.Archetypes),
            AgeGroup = request.AgeGroup,
            MinimumAge = request.MinimumAge,
            CoreAxes = ParseCoreAxesOrThrow(request.CoreAxes),
            Characters = request.Characters,
            Scenes = request.Scenes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Scenarios.Add(scenario);

        // Validate the full scenario model before persisting
        await ValidateScenarioAsync(scenario);

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
        {
            return null;
        }

        ValidateAgainstSchema(request);

        scenario.Title = request.Title;
        scenario.Description = request.Description;
        scenario.Tags = request.Tags;
        scenario.Difficulty = request.Difficulty;
        scenario.SessionLength = request.SessionLength;
        scenario.Archetypes = ParseArchetypesOrThrow(request.Archetypes);
        scenario.AgeGroup = request.AgeGroup;
        scenario.MinimumAge = request.MinimumAge;
        scenario.CoreAxes = ParseCoreAxesOrThrow(request.CoreAxes);
        scenario.Characters = request.Characters;
        scenario.Scenes = request.Scenes;

        await ValidateScenarioAsync(scenario);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }

    private static List<Archetype> ParseArchetypesOrThrow(List<string>? values)
    {
        var result = new List<Archetype>();
        if (values == null)
        {
            return result;
        }

        foreach (var v in values)
        {
            var parsed = Archetype.Parse(v);
            if (parsed == null)
            {
                throw new ArgumentException($"Unknown archetype '{v}'.");
            }
            result.Add(parsed);
        }

        return result;
    }

    private static List<CoreAxis> ParseCoreAxesOrThrow(List<string>? values)
    {
        var result = new List<CoreAxis>();
        if (values == null)
        {
            return result;
        }

        foreach (var v in values)
        {
            var parsed = CoreAxis.Parse(v);
            if (parsed == null)
            {
                throw new ArgumentException($"Unknown core axis '{v}'.");
            }
            result.Add(parsed);
        }

        return result;
    }

    public async Task<bool> DeleteScenarioAsync(string id)
    {
        var scenario = await _context.Scenarios.FirstOrDefaultAsync(s => s.Id == id);
        if (scenario == null)
        {
            return false;
        }

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
            request.Title,
            request.Description,
            Tags = tags,
            Difficulty = request.Difficulty.ToString(),
            SessionLength = request.SessionLength.ToString(),
            request.AgeGroup,
            request.MinimumAge,
            CoreAxes = coreAxes,
            Archetypes = archetypes,
            Characters = characters.Select(character =>
            {
                var meta = character.Metadata;
                return new
                {
                    character.Id,
                    character.Name,
                    character.Image,
                    character.Audio,
                    Metadata = meta == null ? null : new
                    {
                        Role = meta.Role ?? new List<string>(),
                        Archetype = (meta.Archetype ?? new List<Archetype>())
                            .Where(a => a != null)
                            .Select(a => a.Value)
                            .ToList(),
                        meta.Species,
                        meta.Age,
                        Traits = meta.Traits ?? new List<string>(),
                        meta.Backstory
                    }
                };
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
                    scene.Id,
                    scene.Title,
                    Type = scene.Type.ToString().ToLowerInvariant(),
                    scene.Description,
                    NextScene = string.IsNullOrWhiteSpace(scene.NextSceneId) ? null : scene.NextSceneId,
                    scene.Difficulty,
                    Media = hasMedia ? new
                    {
                        media?.Image,
                        media?.Audio,
                        media?.Video
                    } : null,
                    Branches = branches.Select(branch => new
                    {
                        branch.Choice,
                        NextScene = string.IsNullOrWhiteSpace(branch.NextSceneId) ? null : branch.NextSceneId,
                        EchoLog = branch.EchoLog == null ? null : new
                        {
                            // Schema expects a string for echo_type
                            EchoType = branch.EchoLog.EchoType?.Value,
                            branch.EchoLog.Description,
                            branch.EchoLog.Strength
                        },
                        CompassChange = branch.CompassChange == null ? null : new
                        {
                            branch.CompassChange.Axis,
                            branch.CompassChange.Delta,
                            branch.CompassChange.DevelopmentalLink
                        }
                    }).ToList(),
                    EchoReveals = echoReveals.Select(reveal => new
                    {
                        // Schema expects a string for echo_type
                        EchoType = reveal.EchoType?.Value,
                        reveal.MinStrength,
                        reveal.TriggerSceneId,
                        reveal.MaxAgeScenes,
                        reveal.RevealMechanic,
                        reveal.Required
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

    public Task ValidateScenarioAsync(Scenario scenario)
    {
        try
        {
            // Validate basic scenario structure
            if (string.IsNullOrWhiteSpace(scenario.Title))
            {
                throw new ScenarioValidationException("Scenario title cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(scenario.Description))
            {
                throw new ScenarioValidationException("Scenario description cannot be empty");
            }

            if (!scenario.Scenes.Any())
            {
                throw new ScenarioValidationException("Scenario must contain at least one scene");
            }

            // Validate scene structure
            foreach (var scene in scenario.Scenes)
            {
                if (string.IsNullOrWhiteSpace(scene.Id))
                {
                    throw new ScenarioValidationException($"Scene is missing an ID (Title: {scene.Title})");
                }

                if (string.IsNullOrWhiteSpace(scene.Title))
                {
                    throw new ScenarioValidationException($"Scene is missing a title (ID: {scene.Id})");
                }

                // Only choice scenes can have echo logs
                if (scene.Type != SceneType.Choice && scene.Branches.Any(b => b.EchoLog != null))
                {
                    throw new ScenarioValidationException($"Only choice scenes can have echo logs (Scene ID: {scene.Id})");
                }

                // Validate echo log values
                foreach (var branch in scene.Branches.Where(b => b.EchoLog != null))
                {
                    var echo = branch.EchoLog!;
                    if (echo.Strength < 0.1 || echo.Strength > 1.0)
                    {
                        throw new ScenarioValidationException($"Echo log strength must be between 0.1 and 1.0 (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }

                    if (EchoType.Parse(echo.EchoType.Value) == null)
                    {
                        throw new ScenarioValidationException($"Invalid echo type '{echo.EchoType}' (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }

                    if (string.IsNullOrWhiteSpace(echo.Description))
                    {
                        throw new ScenarioValidationException($"Echo log description cannot be empty (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }
                }

                // Validate compass changes
                foreach (var branch in scene.Branches.Where(b => b.CompassChange != null))
                {
                    var change = branch.CompassChange!;
                    if (change.Delta < -1.0 || change.Delta > 1.0)
                    {
                        throw new ScenarioValidationException($"Compass change delta must be between -1.0 and 1.0 (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }

                    if (string.IsNullOrWhiteSpace(change.Axis))
                    {
                        throw new ScenarioValidationException($"Compass axis cannot be empty (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }

                    if (!scenario.CoreAxes.Select(a => a.Value).Contains(change.Axis))
                    {
                        // TODO: Enhancement - Re-enable strict validation when master axis list is finalized
                        // This will ensure all compass axes referenced in scenarios are valid according to the domain model
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
                    {
                        throw new ScenarioValidationException($"Branch references non-existent next scene ID '{branch.NextSceneId}' (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                    }
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

        return Task.CompletedTask;
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
            // Convert Api.Models.Character to Domain.Models.CharacterMapFileCharacter
            var allCharacters = characterMapFile.Characters.ToDictionary(
                c => c.Id,
                c => new CharacterMapFileCharacter
                {
                    Id = c.Id,
                    Name = c.Name,
                    Image = c.Image,
                    Metadata = new CharacterMetadata
                    {
                        Roles = c.Metadata.Roles,
                        Archetypes = c.Metadata.Archetypes,
                        Species = c.Metadata.Species,
                        Age = c.Metadata.Age,
                        Traits = c.Metadata.Traits,
                        Backstory = c.Metadata.Backstory
                    }
                });

            MediaMetadataFile? mediaMetadata = null;
            CharacterMediaMetadataFile? characterMetadata = null;

            if (includeMetadataValidation)
            {
                try
                {
                    var apiMediaMetadata = await _mediaMetadataService.GetMediaMetadataFileAsync();
                    var apiCharacterMetadata = await _characterMetadataService.GetCharacterMediaMetadataFileAsync();

                    // Convert Api.Models to Domain.Models
                    mediaMetadata = apiMediaMetadata == null ? null : new MediaMetadataFile
                    {
                        Id = apiMediaMetadata.Id,
                        Entries = apiMediaMetadata.Entries.Select(e => new MediaMetadataEntry
                        {
                            Id = e.Id,
                            Title = e.Title,
                            FileName = e.FileName,
                            Type = e.Type,
                            Description = e.Description,
                            AgeRating = e.AgeRating,
                            SubjectReferenceId = e.SubjectReferenceId,
                            ClassificationTags = e.ClassificationTags.Select(t => new ClassificationTag { Key = t.Key, Value = t.Value }).ToList(),
                            Modifiers = e.Modifiers.Select(m => new Modifier { Key = m.Key, Value = m.Value }).ToList(),
                            Loopable = e.Loopable
                        }).ToList(),
                        CreatedAt = apiMediaMetadata.CreatedAt,
                        UpdatedAt = apiMediaMetadata.UpdatedAt,
                        Version = apiMediaMetadata.Version
                    };

                    characterMetadata = apiCharacterMetadata == null ? null : new CharacterMediaMetadataFile
                    {
                        Id = apiCharacterMetadata.Id,
                        Entries = apiCharacterMetadata.Entries.Select(e => new CharacterMediaMetadataEntry
                        {
                            Id = e.Id,
                            Title = e.Title,
                            FileName = e.FileName,
                            Type = e.Type,
                            Description = e.Description,
                            AgeRating = e.AgeRating,
                            Tags = e.Tags,
                            Loopable = e.Loopable
                        }).ToList(),
                        CreatedAt = apiCharacterMetadata.CreatedAt,
                        UpdatedAt = apiCharacterMetadata.UpdatedAt,
                        Version = apiCharacterMetadata.Version
                    };
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
        Dictionary<string, CharacterMapFileCharacter> allCharacters,
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

    private Task ValidateMediaReference(
        Scene scene,
        string? mediaId,
        string mediaType,
        Dictionary<string, MediaAsset> allMedia,
        MediaMetadataFile? mediaMetadata,
        ScenarioReferenceValidation validation,
        bool includeMetadataValidation)
    {
        if (string.IsNullOrEmpty(mediaId))
        {
            return Task.CompletedTask;
        }

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

        return Task.CompletedTask;
    }

    private Task ValidateCharacterReferences(
        Scene scene,
        Dictionary<string, CharacterMapFileCharacter> allCharacters,
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

        return Task.CompletedTask;
    }

    // Define a custom exception for scenario validation errors
    public class ScenarioValidationException : Exception
    {
        public ScenarioValidationException(string message) : base(message) { }
        public ScenarioValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
