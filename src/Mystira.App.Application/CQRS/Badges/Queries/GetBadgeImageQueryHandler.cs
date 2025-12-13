using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed class GetBadgeImageQueryHandler : IQueryHandler<GetBadgeImageQuery, BadgeImageResult?>
{
    private readonly IBadgeImageRepository _badgeImageRepository;

    public GetBadgeImageQueryHandler(IBadgeImageRepository badgeImageRepository)
    {
        _badgeImageRepository = badgeImageRepository;
    }

    public async Task<BadgeImageResult?> Handle(GetBadgeImageQuery request, CancellationToken cancellationToken)
    {
        var decodedId = Uri.UnescapeDataString(request.ImageId);

        var image = await _badgeImageRepository.GetByImageIdAsync(decodedId)
                    ?? await _badgeImageRepository.GetByIdAsync(decodedId);

        if (image?.ImageData is not { Length: > 0 }) return null;

        return new BadgeImageResult(image.ImageData, image.ContentType);
    }
}
