using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IBadgeAdminService
{
    Task<IReadOnlyList<BadgeDto>> GetBadgesAsync(BadgeQueryOptions options);
    Task<BadgeDto?> GetBadgeByIdAsync(string id);
    Task<BadgeDto> CreateBadgeAsync(CreateBadgeRequest request);
    Task<BadgeDto?> UpdateBadgeAsync(string id, UpdateBadgeRequest request);
    Task<bool> DeleteBadgeAsync(string id);

    Task<IReadOnlyList<AxisAchievementDto>> GetAxisAchievementsAsync(string? ageGroupId, string? compassAxisId);
    Task<AxisAchievementDto> CreateAxisAchievementAsync(AxisAchievementRequest request);
    Task<AxisAchievementDto?> UpdateAxisAchievementAsync(string id, AxisAchievementRequest request);
    Task<bool> DeleteAxisAchievementAsync(string id);

    Task<BadgeSnapshotDto?> GetSnapshotAsync(string ageGroupId);
    Task<BadgeImportResult> ImportAsync(Stream configStream, bool overwrite);

    Task<IReadOnlyList<BadgeImageDto>> SearchImagesAsync(string? imageId, bool includeData = true);
    Task<BadgeImageDto?> GetImageAsync(string idOrImageId, bool includeData = true);
    Task<BadgeImageDto> UploadImageAsync(string imageId, IFormFile file);
    Task<bool> DeleteImageAsync(string id);
}
