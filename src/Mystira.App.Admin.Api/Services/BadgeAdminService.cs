using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using NJsonSchema;

namespace Mystira.App.Admin.Api.Services;

public class BadgeAdminService : IBadgeAdminService
{
    private readonly MystiraAppDbContext _context;
    private readonly IAxisAchievementRepository _axisAchievementRepository;
    private readonly IBadgeRepository _badgeRepository;
    private readonly IBadgeImageRepository _badgeImageRepository;
    private readonly ICompassAxisRepository _compassAxisRepository;
    private readonly IAgeGroupRepository _ageGroupRepository;
    private readonly ILogger<BadgeAdminService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private JsonSchema? _badgeSchema;

    private static readonly string[] TierOrder = ["Bronze", "Silver", "Gold", "Platinum", "Diamond"];
    private static readonly HashSet<string> TierLookup = new(TierOrder, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> AxisDirectionLookup = new(["positive", "negative"], StringComparer.OrdinalIgnoreCase);
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private const int MaxImageSearchResults = 75;

    public BadgeAdminService(
        MystiraAppDbContext context,
        IAxisAchievementRepository axisAchievementRepository,
        IBadgeRepository badgeRepository,
        IBadgeImageRepository badgeImageRepository,
        ICompassAxisRepository compassAxisRepository,
        IAgeGroupRepository ageGroupRepository,
        ILogger<BadgeAdminService> logger)
    {
        _context = context;
        _axisAchievementRepository = axisAchievementRepository;
        _badgeRepository = badgeRepository;
        _badgeImageRepository = badgeImageRepository;
        _compassAxisRepository = compassAxisRepository;
        _ageGroupRepository = ageGroupRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BadgeDto>> GetBadgesAsync(BadgeQueryOptions options)
    {
        _logger.LogInformation("Fetching badges (AgeGroup={AgeGroup}, Axis={Axis}, Tier={Tier})", options.AgeGroupId, options.CompassAxisId, options.Tier);

        var query = _context.Badges.AsQueryable();

        if (!string.IsNullOrWhiteSpace(options.AgeGroupId))
        {
            var normalizedAgeGroup = await NormalizeAgeGroupValueAsync(options.AgeGroupId);
            if (!string.IsNullOrEmpty(normalizedAgeGroup))
            {
                query = query.Where(b => b.AgeGroupId == normalizedAgeGroup);
            }
        }

        if (!string.IsNullOrWhiteSpace(options.CompassAxisId))
        {
            var axisFilter = options.CompassAxisId.Trim().ToLowerInvariant();
            query = query.Where(b => b.CompassAxisId != null && b.CompassAxisId.ToLower() == axisFilter);
        }

        if (!string.IsNullOrWhiteSpace(options.Tier))
        {
            var tierFilter = options.Tier.Trim().ToLowerInvariant();
            query = query.Where(b => b.Tier != null && b.Tier.ToLower() == tierFilter);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim().ToLowerInvariant();
            query = query.Where(b => b.Title.ToLower().Contains(search) || b.Description.ToLower().Contains(search));
        }

        // Cosmos DB requires a composite index for multi-property OrderBy.
        // To avoid BadRequest (400) due to missing composite index, materialize first
        // and then perform ordering in-memory. Filters still execute server-side.
        var filtered = await query.ToListAsync();
        var badges = filtered
            .OrderBy(b => b.AgeGroupId)
            .ThenBy(b => b.CompassAxisId)
            .ThenBy(b => b.TierOrder)
            .ToList();

        return await MapBadgesAsync(badges, options.IncludeAxisMetadata, options.IncludeImages);
    }

    public async Task<BadgeDto?> GetBadgeByIdAsync(string id)
    {
        var badge = await _badgeRepository.GetByIdAsync(id);
        if (badge == null)
        {
            return null;
        }

        var mapped = await MapBadgesAsync(new[] { badge }, includeAxisMetadata: true, includeImages: true);
        return mapped.FirstOrDefault();
    }

    public async Task<BadgeDto> CreateBadgeAsync(CreateBadgeRequest request)
    {
        var ageGroup = await RequireAgeGroupAsync(request.AgeGroupId);
        var axis = await RequireAxisAsync(request.CompassAxisId);
        var tier = NormalizeTier(request.Tier);
        ValidateBadgeNumbers(request.TierOrder, request.RequiredScore);

        var badge = new Badge
        {
            Id = Guid.NewGuid().ToString(),
            AgeGroupId = ageGroup.Value,
            CompassAxisId = NormalizeAxisValue(axis, request.CompassAxisId),
            Tier = tier,
            TierOrder = request.TierOrder,
            RequiredScore = request.RequiredScore,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ImageId = request.ImageId.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _badgeRepository.AddAsync(badge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created badge {BadgeId} for age group {AgeGroup}", badge.Id, badge.AgeGroupId);

        return (await GetBadgeByIdAsync(badge.Id))!;
    }

    public async Task<BadgeDto?> UpdateBadgeAsync(string id, UpdateBadgeRequest request)
    {
        var badge = await _badgeRepository.GetByIdAsync(id);
        if (badge == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.AgeGroupId))
        {
            var ageGroup = await RequireAgeGroupAsync(request.AgeGroupId);
            badge.AgeGroupId = ageGroup.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.CompassAxisId))
        {
            var axis = await RequireAxisAsync(request.CompassAxisId);
            badge.CompassAxisId = NormalizeAxisValue(axis, request.CompassAxisId);
        }

        if (!string.IsNullOrWhiteSpace(request.Tier))
        {
            badge.Tier = NormalizeTier(request.Tier);
        }

        if (request.TierOrder.HasValue)
        {
            if (request.TierOrder.Value < 1)
            {
                throw new ArgumentException("Tier order must be greater than zero");
            }
            badge.TierOrder = request.TierOrder.Value;
        }

        if (request.RequiredScore.HasValue)
        {
            if (request.RequiredScore.Value <= 0)
            {
                throw new ArgumentException("Required score must be greater than zero");
            }
            badge.RequiredScore = request.RequiredScore.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            badge.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            badge.Description = request.Description.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ImageId))
        {
            badge.ImageId = request.ImageId.Trim();
        }

        badge.UpdatedAt = DateTime.UtcNow;
        await _badgeRepository.UpdateAsync(badge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated badge {BadgeId}", badge.Id);

        return await GetBadgeByIdAsync(badge.Id);
    }

    public async Task<bool> DeleteBadgeAsync(string id)
    {
        var existing = await _badgeRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        await _badgeRepository.DeleteAsync(id);
        var affected = await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted badge {BadgeId}", id);
        return affected > 0;
    }

    public async Task<IReadOnlyList<AxisAchievementDto>> GetAxisAchievementsAsync(string? ageGroupId, string? compassAxisId)
    {
        var query = _context.AxisAchievements.AsQueryable();

        if (!string.IsNullOrWhiteSpace(ageGroupId))
        {
            var normalizedAgeGroup = await NormalizeAgeGroupValueAsync(ageGroupId);
            if (!string.IsNullOrEmpty(normalizedAgeGroup))
            {
                query = query.Where(a => a.AgeGroupId == normalizedAgeGroup);
            }
        }

        if (!string.IsNullOrWhiteSpace(compassAxisId))
        {
            var axisFilter = compassAxisId.Trim().ToLowerInvariant();
            query = query.Where(a => a.CompassAxisId.ToLower() == axisFilter);
        }

        // Cosmos DB requires a composite index for multi-property OrderBy.
        // To avoid BadRequest (400) due to missing composite index, materialize first
        // and then perform ordering in-memory. Filters still execute server-side.
        var filtered = await query.ToListAsync();
        var entities = filtered
            .OrderBy(a => a.AgeGroupId)
            .ThenBy(a => a.CompassAxisId)
            .ToList();

        var axisLookup = await GetAxisLookupAsync();

        return entities.Select(a => MapAxisAchievement(a, axisLookup)).ToList();
    }

    public async Task<AxisAchievementDto> CreateAxisAchievementAsync(AxisAchievementRequest request)
    {
        var ageGroup = await RequireAgeGroupAsync(request.AgeGroupId);
        var axis = await RequireAxisAsync(request.CompassAxisId);
        var direction = NormalizeDirection(request.AxesDirection);

        var entity = new AxisAchievement
        {
            Id = Guid.NewGuid().ToString(),
            AgeGroupId = ageGroup.Value,
            CompassAxisId = NormalizeAxisValue(axis, request.CompassAxisId),
            AxesDirection = direction,
            Description = request.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _axisAchievementRepository.AddAsync(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created axis achievement {AxisAchievementId}", entity.Id);

        var axisLookup = await GetAxisLookupAsync();
        return MapAxisAchievement(entity, axisLookup);
    }

    public async Task<AxisAchievementDto?> UpdateAxisAchievementAsync(string id, AxisAchievementRequest request)
    {
        var entity = await _axisAchievementRepository.GetByIdAsync(id);
        if (entity == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.AgeGroupId))
        {
            var ageGroup = await RequireAgeGroupAsync(request.AgeGroupId);
            entity.AgeGroupId = ageGroup.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.CompassAxisId))
        {
            var axis = await RequireAxisAsync(request.CompassAxisId);
            entity.CompassAxisId = NormalizeAxisValue(axis, request.CompassAxisId);
        }

        if (!string.IsNullOrWhiteSpace(request.AxesDirection))
        {
            entity.AxesDirection = NormalizeDirection(request.AxesDirection);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            entity.Description = request.Description.Trim();
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _axisAchievementRepository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        var axisLookup = await GetAxisLookupAsync();
        return MapAxisAchievement(entity, axisLookup);
    }

    public async Task<bool> DeleteAxisAchievementAsync(string id)
    {
        var existing = await _axisAchievementRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        await _axisAchievementRepository.DeleteAsync(id);
        var affected = await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted axis achievement {AxisAchievementId}", id);
        return affected > 0;
    }

    public async Task<BadgeSnapshotDto?> GetSnapshotAsync(string ageGroupId)
    {
        var ageGroup = await ResolveAgeGroupAsync(ageGroupId);
        if (ageGroup == null)
        {
            return null;
        }

        var axisAchievements = await GetAxisAchievementsAsync(ageGroup.Value, null);
        var badges = await GetBadgesAsync(new BadgeQueryOptions
        {
            AgeGroupId = ageGroup.Value,
            IncludeAxisMetadata = true,
            IncludeImages = true
        });

        return new BadgeSnapshotDto
        {
            AgeGroupId = ageGroup.Value,
            AgeGroupName = ageGroup.Name,
            MinimumAge = ageGroup.MinimumAge,
            MaximumAge = ageGroup.MaximumAge,
            AxisAchievements = axisAchievements.ToList(),
            Badges = badges.ToList()
        };
    }

    public async Task<BadgeImportResult> ImportAsync(Stream configStream, bool overwrite)
    {
        using var reader = new StreamReader(configStream);
        var json = await reader.ReadToEndAsync();

        var result = new BadgeImportResult
        {
            Success = false,
            Overwrite = overwrite
        };

        var schema = await EnsureBadgeSchemaAsync();
        if (schema == null)
        {
            result.Errors.Add("Badge configuration schema could not be located on the server.");
            return result;
        }

        var validationErrors = schema.Validate(json);
        if (validationErrors.Any())
        {
            foreach (var error in validationErrors)
            {
                result.Errors.Add(error.ToString());
            }
            return result;
        }

        var config = JsonSerializer.Deserialize<BadgeConfigurationFile>(json, _jsonOptions);
        if (config == null)
        {
            result.Errors.Add("Unable to parse badge configuration file.");
            return result;
        }

        result.AgeGroupId = config.AgeGroupId;

        var ageGroup = await ResolveAgeGroupAsync(config.AgeGroupId);
        if (ageGroup == null)
        {
            result.Errors.Add($"Age group '{config.AgeGroupId}' is not defined.");
            return result;
        }

        var axisLookups = await GetAxisLookupAsync(includeDeleted: false);
        var axisValidationErrors = (config.AxisAchievements ?? new List<AxisAchievementFile>())
            .Select(a => a.CompassAxisId)
            .Concat((config.Badges ?? new List<BadgeFile>()).Select(b => b.CompassAxisId))
            .Where(axisId => !string.IsNullOrWhiteSpace(axisId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(axisId => !axisLookups.ContainsKey(axisId))
            .ToList();
        if (axisValidationErrors.Any())
        {
            foreach (var axisId in axisValidationErrors)
            {
                result.Errors.Add($"Compass axis '{axisId}' is not defined.");
            }
            return result;
        }

        var normalizedAchievements = config.AxisAchievements ?? new List<AxisAchievementFile>();
        var normalizedBadges = config.Badges ?? new List<BadgeFile>();

        if (overwrite)
        {
            var axisToRemove = await _context.AxisAchievements.Where(a => a.AgeGroupId == ageGroup.Value).ToListAsync();
            var badgesToRemove = await _context.Badges.Where(b => b.AgeGroupId == ageGroup.Value).ToListAsync();
            _context.AxisAchievements.RemoveRange(axisToRemove);
            _context.Badges.RemoveRange(badgesToRemove);
            await _context.SaveChangesAsync();
        }

        var existingAxisAchievements = await _context.AxisAchievements.Where(a => a.AgeGroupId == ageGroup.Value).ToListAsync();
        var existingBadges = await _context.Badges.Where(b => b.AgeGroupId == ageGroup.Value).ToListAsync();

        foreach (var achievement in normalizedAchievements)
        {
            var direction = NormalizeDirection(achievement.AxesDirection);
            axisLookups.TryGetValue(achievement.CompassAxisId, out var axisReference);
            var axisName = NormalizeAxisValue(axisReference, achievement.CompassAxisId);

            var existing = existingAxisAchievements.FirstOrDefault(a =>
                string.Equals(a.CompassAxisId, axisName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.AxesDirection, direction, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                var entity = new AxisAchievement
                {
                    Id = Guid.NewGuid().ToString(),
                    AgeGroupId = ageGroup.Value,
                    CompassAxisId = axisName,
                    AxesDirection = direction,
                    Description = achievement.Description.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _axisAchievementRepository.AddAsync(entity);
                existingAxisAchievements.Add(entity);
                result.CreatedAxisAchievements++;
            }
            else
            {
                existing.Description = achievement.Description.Trim();
                existing.AxesDirection = direction;
                existing.UpdatedAt = DateTime.UtcNow;
                await _axisAchievementRepository.UpdateAsync(existing);
                result.UpdatedAxisAchievements++;
            }
        }

        foreach (var badgeConfig in normalizedBadges)
        {
            var tier = NormalizeTier(badgeConfig.Tier);
            ValidateBadgeNumbers(badgeConfig.Tier_Order, badgeConfig.Required_Score);
            axisLookups.TryGetValue(badgeConfig.CompassAxisId, out var axisReference);
            var axisName = NormalizeAxisValue(axisReference, badgeConfig.CompassAxisId);

            var existing = existingBadges.FirstOrDefault(b =>
                string.Equals(b.CompassAxisId, axisName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Tier, tier, StringComparison.OrdinalIgnoreCase) &&
                b.TierOrder == badgeConfig.Tier_Order);

            if (existing == null)
            {
                var badge = new Badge
                {
                    Id = Guid.NewGuid().ToString(),
                    AgeGroupId = ageGroup.Value,
                    CompassAxisId = axisName,
                    Tier = tier,
                    TierOrder = badgeConfig.Tier_Order,
                    Title = badgeConfig.Title.Trim(),
                    Description = badgeConfig.Description.Trim(),
                    RequiredScore = badgeConfig.Required_Score,
                    ImageId = badgeConfig.Image_Id.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _badgeRepository.AddAsync(badge);
                existingBadges.Add(badge);
                result.CreatedBadges++;
            }
            else
            {
                existing.Title = badgeConfig.Title.Trim();
                existing.Description = badgeConfig.Description.Trim();
                existing.RequiredScore = badgeConfig.Required_Score;
                existing.ImageId = badgeConfig.Image_Id.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
                await _badgeRepository.UpdateAsync(existing);
                result.UpdatedBadges++;
            }
        }

        await _context.SaveChangesAsync();
        result.Success = !result.Errors.Any();

        _logger.LogInformation("Imported badge configuration for age group {AgeGroup} (CreatedBadges={CreatedBadges}, UpdatedBadges={UpdatedBadges})",
            ageGroup.Value, result.CreatedBadges, result.UpdatedBadges);

        return result;
    }

    public async Task<IReadOnlyList<BadgeImageDto>> SearchImagesAsync(string? imageId, bool includeData = true)
    {
        var query = _context.BadgeImages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(imageId))
        {
            var search = imageId.Trim().ToLowerInvariant();
            query = query.Where(i => i.ImageId.ToLower().Contains(search));
        }

        var images = await query
            .OrderBy(i => i.ImageId)
            .Take(MaxImageSearchResults)
            .ToListAsync();

        return images.Select(i => MapBadgeImage(i, includeData)).ToList();
    }

    public async Task<BadgeImageDto?> GetImageAsync(string idOrImageId, bool includeData = true)
    {
        var entity = await _badgeImageRepository.GetByIdAsync(idOrImageId)
            ?? await _badgeImageRepository.GetByImageIdAsync(idOrImageId);

        return entity == null ? null : MapBadgeImage(entity, includeData);
    }

    public async Task<BadgeImageDto> UploadImageAsync(string imageId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Image upload failed: file is missing or empty");
        }

        if (file.Length > MaxImageSizeBytes)
        {
            throw new ArgumentException($"Badge images must be smaller than {MaxImageSizeBytes / (1024 * 1024)} MB");
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();

        var normalizedImageId = imageId.Trim();
        var existing = await _badgeImageRepository.GetByImageIdAsync(normalizedImageId)
            ?? await _badgeImageRepository.GetByIdAsync(imageId);

        if (existing == null)
        {
            existing = new BadgeImage
            {
                Id = Guid.NewGuid().ToString(),
                ImageId = normalizedImageId,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType,
                ImageData = data,
                FileSizeBytes = data.LongLength,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _badgeImageRepository.AddAsync(existing);
        }
        else
        {
            existing.ImageId = normalizedImageId;
            existing.ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? existing.ContentType : file.ContentType;
            existing.ImageData = data;
            existing.FileSizeBytes = data.LongLength;
            existing.UpdatedAt = DateTime.UtcNow;
            await _badgeImageRepository.UpdateAsync(existing);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Uploaded badge image {ImageId}", normalizedImageId);

        return MapBadgeImage(existing, includeData: true);
    }

    public async Task<bool> DeleteImageAsync(string id)
    {
        var entity = await _badgeImageRepository.GetByIdAsync(id)
            ?? await _badgeImageRepository.GetByImageIdAsync(id);

        if (entity == null)
        {
            return false;
        }

        await _badgeImageRepository.DeleteAsync(entity.Id);
        var affected = await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted badge image {ImageId}", entity.ImageId);
        return affected > 0;
    }

    private async Task<IReadOnlyList<BadgeDto>> MapBadgesAsync(IEnumerable<Badge> badges, bool includeAxisMetadata, bool includeImages)
    {
        var list = badges.ToList();
        Dictionary<string, AgeGroupDefinition>? ageGroupLookup = null;
        Dictionary<string, CompassAxis>? axisLookup = null;
        Dictionary<string, BadgeImage>? imageLookup = null;

        if (includeAxisMetadata)
        {
            ageGroupLookup = (await _ageGroupRepository.GetAllAsync()).ToDictionary(a => a.Value, StringComparer.OrdinalIgnoreCase);
            axisLookup = await GetAxisLookupAsync();
        }

        if (includeImages)
        {
            var imageIds = list
                .Where(b => !string.IsNullOrWhiteSpace(b.ImageId))
                .Select(b => b.ImageId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (imageIds.Any())
            {
                imageLookup = await _context.BadgeImages
                    .Where(img => imageIds.Contains(img.ImageId))
                    .ToDictionaryAsync(img => img.ImageId, StringComparer.OrdinalIgnoreCase);
            }
        }

        return list.Select(b => MapBadge(b, ageGroupLookup, axisLookup, imageLookup)).ToList();
    }

    private static BadgeDto MapBadge(
        Badge badge,
        Dictionary<string, AgeGroupDefinition>? ageGroupLookup,
        Dictionary<string, CompassAxis>? axisLookup,
        Dictionary<string, BadgeImage>? imageLookup)
    {
        var dto = new BadgeDto
        {
            Id = badge.Id,
            AgeGroupId = badge.AgeGroupId,
            CompassAxisId = badge.CompassAxisId,
            Tier = badge.Tier,
            TierOrder = badge.TierOrder,
            RequiredScore = badge.RequiredScore,
            Title = badge.Title,
            Description = badge.Description,
            ImageId = badge.ImageId,
            CreatedAt = badge.CreatedAt,
            UpdatedAt = badge.UpdatedAt
        };

        if (ageGroupLookup != null && ageGroupLookup.TryGetValue(badge.AgeGroupId, out var ageGroup))
        {
            dto.AgeGroupName = string.IsNullOrWhiteSpace(ageGroup.Name)
                ? AgeGroupConstants.GetDisplayName(ageGroup.Value)
                : ageGroup.Name;
            dto.MinimumAge = ageGroup.MinimumAge;
            dto.MaximumAge = ageGroup.MaximumAge;
        }
        else
        {
            dto.AgeGroupName = AgeGroupConstants.GetDisplayName(badge.AgeGroupId);
        }

        dto.CompassAxisName = (axisLookup != null && axisLookup.TryGetValue(badge.CompassAxisId, out var axis))
            ? (string.IsNullOrWhiteSpace(axis.Name) ? axis.Id : axis.Name)
            : badge.CompassAxisId;

        if (imageLookup != null && imageLookup.TryGetValue(badge.ImageId, out var image))
        {
            dto.Image = MapBadgeImage(image, includeData: true);
        }

        return dto;
    }

    private static AxisAchievementDto MapAxisAchievement(AxisAchievement entity, Dictionary<string, CompassAxis> axisLookup)
    {
        var dto = new AxisAchievementDto
        {
            Id = entity.Id,
            AgeGroupId = entity.AgeGroupId,
            CompassAxisId = entity.CompassAxisId,
            AxesDirection = entity.AxesDirection,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        dto.CompassAxisName = axisLookup.TryGetValue(entity.CompassAxisId, out var axis)
            ? (string.IsNullOrWhiteSpace(axis.Name) ? axis.Id : axis.Name)
            : entity.CompassAxisId;

        return dto;
    }

    private static BadgeImageDto MapBadgeImage(BadgeImage entity, bool includeData)
    {
        var dto = new BadgeImageDto
        {
            Id = entity.Id,
            ImageId = entity.ImageId,
            ContentType = entity.ContentType,
            FileSizeBytes = entity.FileSizeBytes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        if (includeData && entity.ImageData is { Length: > 0 })
        {
            var base64 = Convert.ToBase64String(entity.ImageData);
            dto.DataUrl = $"data:{entity.ContentType};base64,{base64}";
        }

        return dto;
    }

    private async Task<JsonSchema?> EnsureBadgeSchemaAsync()
    {
        if (_badgeSchema != null)
        {
            return _badgeSchema;
        }

        var schemaPath = ResolveSchemaPath();
        if (schemaPath == null)
        {
            return null;
        }

        var schemaJson = await File.ReadAllTextAsync(schemaPath);
        _badgeSchema = await JsonSchema.FromJsonAsync(schemaJson);
        return _badgeSchema;
    }

    private static string? ResolveSchemaPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(baseDirectory, "badge-configuration.schema.json"),
            Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "src", "Mystira.App.Domain", "Schemas", "badge-configuration.schema.json")),
            Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "Mystira.App.Domain", "Schemas", "badge-configuration.schema.json"))
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private async Task<AgeGroupDefinition> RequireAgeGroupAsync(string ageGroupId)
    {
        var ageGroup = await ResolveAgeGroupAsync(ageGroupId);
        if (ageGroup == null)
        {
            throw new ArgumentException($"Age group '{ageGroupId}' does not exist.");
        }

        return ageGroup;
    }

    private async Task<CompassAxis> RequireAxisAsync(string axisId)
    {
        var axis = await ResolveAxisAsync(axisId);
        if (axis == null)
        {
            throw new ArgumentException($"Compass axis '{axisId}' does not exist.");
        }

        return axis;
    }

    private async Task<AgeGroupDefinition?> ResolveAgeGroupAsync(string ageGroupId)
    {
        if (string.IsNullOrWhiteSpace(ageGroupId))
        {
            return null;
        }

        var trimmed = ageGroupId.Trim();
        return await _ageGroupRepository.GetByIdAsync(trimmed)
               ?? await _ageGroupRepository.GetByValueAsync(trimmed);
    }

    private async Task<string?> NormalizeAgeGroupValueAsync(string? ageGroupId)
    {
        if (string.IsNullOrWhiteSpace(ageGroupId))
        {
            return null;
        }

        var group = await ResolveAgeGroupAsync(ageGroupId);
        return group?.Value ?? ageGroupId.Trim();
    }

    private async Task<CompassAxis?> ResolveAxisAsync(string axisId)
    {
        if (string.IsNullOrWhiteSpace(axisId))
        {
            return null;
        }

        var trimmed = axisId.Trim();
        return await _compassAxisRepository.GetByIdAsync(trimmed)
               ?? await _compassAxisRepository.GetByNameAsync(trimmed);
    }

    private static string NormalizeAxisValue(CompassAxis? axis, string fallback)
    {
        if (axis == null)
        {
            return fallback.Trim();
        }

        return string.IsNullOrWhiteSpace(axis.Name) ? axis.Id : axis.Name;
    }

    private static void ValidateBadgeNumbers(int tierOrder, float requiredScore)
    {
        if (tierOrder < 1)
        {
            throw new ArgumentException("Tier order must be greater than zero");
        }

        if (requiredScore <= 0)
        {
            throw new ArgumentException("Required score must be greater than zero");
        }
    }

    private static string NormalizeTier(string tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
        {
            throw new ArgumentException("Tier is required");
        }

        var match = TierOrder.FirstOrDefault(t => t.Equals(tier, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            return match;
        }

        var normalized = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(tier.Trim().ToLowerInvariant());
        if (!TierLookup.Contains(normalized))
        {
            throw new ArgumentException($"Tier '{tier}' is not supported. Valid tiers: {string.Join(", ", TierOrder)}");
        }

        return normalized;
    }

    private static string NormalizeDirection(string direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            return "positive";
        }

        var normalized = direction.Trim().ToLowerInvariant();
        if (!AxisDirectionLookup.Contains(normalized))
        {
            throw new ArgumentException("Axis direction must be 'positive' or 'negative'");
        }

        return normalized;
    }

    private async Task<Dictionary<string, CompassAxis>> GetAxisLookupAsync(bool includeDeleted = false)
    {
        var axes = await _compassAxisRepository.GetAllAsync();
        var lookup = new Dictionary<string, CompassAxis>(StringComparer.OrdinalIgnoreCase);

        foreach (var axis in axes)
        {
            if (!includeDeleted && axis.IsDeleted)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(axis.Id))
            {
                lookup[axis.Id] = axis;
            }

            if (!string.IsNullOrWhiteSpace(axis.Name))
            {
                lookup[axis.Name] = axis;
            }
        }

        return lookup;
    }

    private sealed class BadgeConfigurationFile
    {
        [JsonPropertyName("Age_Group_Id")]
        public string AgeGroupId { get; set; } = string.Empty;

        [JsonPropertyName("Axis_Achievements")]
        public List<AxisAchievementFile> AxisAchievements { get; set; } = new();

        [JsonPropertyName("Badges")]
        public List<BadgeFile> Badges { get; set; } = new();
    }

    private sealed class AxisAchievementFile
    {
        [JsonPropertyName("Compass_Axis_Id")]
        public string CompassAxisId { get; set; } = string.Empty;

        [JsonPropertyName("Axes_Direction")]
        public string AxesDirection { get; set; } = string.Empty;

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;
    }

    private sealed class BadgeFile
    {
        [JsonPropertyName("Tier")]
        public string Tier { get; set; } = string.Empty;

        [JsonPropertyName("Tier_Order")]
        public int Tier_Order { get; set; }

        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("Required_Score")]
        public float Required_Score { get; set; }

        [JsonPropertyName("Compass_Axis_Id")]
        public string CompassAxisId { get; set; } = string.Empty;

        [JsonPropertyName("Image_Id")]
        public string Image_Id { get; set; } = string.Empty;
    }
}
