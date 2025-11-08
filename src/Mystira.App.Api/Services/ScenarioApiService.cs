using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Data;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

public class ScenarioApiService : IScenarioApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<ScenarioApiService> _logger;
    private readonly IMediaApiService _mediaService;
    private readonly ICharacterMapFileService _characterService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ICharacterMediaMetadataService _characterMetadataService;

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

        // Apply filters
        if (request.Difficulty.HasValue)
            query = query.Where(s => s.Difficulty == request.Difficulty.Value);

        if (request.SessionLength.HasValue)
            query = query.Where(s => s.SessionLength == request.SessionLength.Value);

        if (!string.IsNullOrEmpty(request.MinimumAge))
        {
            // Filter scenarios with compatible age groups (in-memory filtering)
            var allScenarios = await query.ToListAsync();
            var compatibleScenarios = allScenarios.Where(s => IsAgeGroupCompatible(s.MinimumAge, request.MinimumAge)).ToList();
            var scenarioSummaries = compatibleScenarios
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
                    Archetypes = s.Archetypes,
                    MinimumAge = s.MinimumAge,
                    Summary = s.Summary,
                    CreatedAt = s.CreatedAt
                })
                .ToList();
            
            return new ScenarioListResponse
            {
                Scenarios = scenarioSummaries,
                TotalCount = compatibleScenarios.Count,
                Page = request.Page,
                PageSize = request.PageSize
            };
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
                query = query.Where(s => s.Archetypes.Contains(archetype));
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
                Archetypes = s.Archetypes,
                MinimumAge = s.MinimumAge,
                Summary = s.Summary,
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
        // Validate archetypes are from master list
        // todo validate after updating valid archetypes & axes
        // var invalidArchetypes = request.Archetypes.Except(MasterLists.Archetypes).ToList();
        // if (invalidArchetypes.Any())
        //     throw new ArgumentException($"Invalid archetypes: {string.Join(", ", invalidArchetypes)}");
        //
        // // Validate compass axes are from master list
        // var invalidAxes = request.CompassAxes.Except(MasterLists.CompassAxes).ToList();
        // if (invalidAxes.Any())
        //     throw new ArgumentException($"Invalid compass axes: {string.Join(", ", invalidAxes)}");

        var scenario = new Scenario
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
            Difficulty = request.Difficulty,
            SessionLength = request.SessionLength,
            Archetypes = request.Archetypes,
            MinimumAge = request.MinimumAge,
            Summary = request.Summary,
            Scenes = request.Scenes,
            CompassAxes = request.CompassAxes,
            CreatedAt = DateTime.UtcNow
        };

        // Validate scenario structure
        await ValidateScenarioAsync(scenario);

        _context.Scenarios.Add(scenario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving scenario: {ScenarioId}", scenario.Id);
        }

        _logger.LogInformation("Created new scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }

    public async Task<Scenario?> UpdateScenarioAsync(string id, CreateScenarioRequest request)
    {
        var scenario = await _context.Scenarios.FirstOrDefaultAsync(s => s.Id == id);
        if (scenario == null)
            return null;

        // Apply updates
        scenario.Title = request.Title;
        scenario.Description = request.Description;
        scenario.Tags = request.Tags;
        scenario.Difficulty = request.Difficulty;
        scenario.SessionLength = request.SessionLength;
        scenario.Archetypes = request.Archetypes;
        scenario.MinimumAge = request.MinimumAge;
        scenario.Summary = request.Summary;
        scenario.Scenes = request.Scenes;
        scenario.CompassAxes = request.CompassAxes;

        // Validate updated scenario
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
        // Get all scenarios and filter in memory for age compatibility
        var scenarios = await _context.Scenarios.ToListAsync();
        
        return scenarios
            .Where(s => IsAgeGroupCompatible(s.MinimumAge, ageGroup))
            .OrderBy(s => s.Title)
            .ToList();
    }

    private bool IsAgeGroupCompatible(string minimumAge, string targetAge)
    {
        // Define age group hierarchy (from youngest to oldest)
        var ageOrder = new List<string> 
        { 
            AgeGroup.Toddlers.Name, 
            AgeGroup.Preschoolers.Name, 
            AgeGroup.School.Name, 
            AgeGroup.Preteens.Name, 
            AgeGroup.Teens.Name 
        };

        var minIndex = ageOrder.IndexOf(minimumAge);
        var targetIndex = ageOrder.IndexOf(targetAge);

        // If either age group is not found, assume compatible for backward compatibility
        if (minIndex == -1 || targetIndex == -1)
            return true;

        // Target age group must be at or above the minimum age group
        return targetIndex >= minIndex;
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

                    // todo update master list
                    // if (!MasterLists.EchoTypes.Contains(echo.EchoType))
                    //     throw new ScenarioValidationException($"Invalid echo type '{echo.EchoType}' (Scene ID: {scene.Id}, Choice: {branch.Choice})");
                        
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

                    if (!scenario.CompassAxes.Contains(change.Axis))
                    {
                        Console.WriteLine(change.Axis);
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