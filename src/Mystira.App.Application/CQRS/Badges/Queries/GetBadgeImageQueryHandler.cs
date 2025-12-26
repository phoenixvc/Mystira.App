using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Badges.Queries;

/// <summary>
/// Wolverine handler for GetBadgeImageQuery.
/// Retrieves badge image data by image ID.
/// </summary>
public static class GetBadgeImageQueryHandler
{
    /// <summary>
    /// Handles the GetBadgeImageQuery by retrieving badge image data from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<BadgeImageResult?> Handle(
        GetBadgeImageQuery query,
        IBadgeImageRepository badgeImageRepository,
        CancellationToken ct)
    {
        var decodedId = Uri.UnescapeDataString(query.ImageId);

        var image = await badgeImageRepository.GetByImageIdAsync(decodedId)
                    ?? await badgeImageRepository.GetByIdAsync(decodedId);

        if (image?.ImageData is not { Length: > 0 }) return null;

        return new BadgeImageResult(image.ImageData, image.ContentType);
    }
}
