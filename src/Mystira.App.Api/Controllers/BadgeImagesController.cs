using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Common;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/badges/images")]
public class BadgeImagesController : ControllerBase
{
    private readonly IBadgeImageRepository _badgeImageRepository;
    private readonly ILogger<BadgeImagesController> _logger;

    public BadgeImagesController(IBadgeImageRepository badgeImageRepository, ILogger<BadgeImagesController> logger)
    {
        _badgeImageRepository = badgeImageRepository;
        _logger = logger;
    }

    [HttpGet("{imageId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBadgeImage(string imageId)
    {
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "imageId is required",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var decodedId = Uri.UnescapeDataString(imageId);

            var image = await _badgeImageRepository.GetByImageIdAsync(decodedId)
                        ?? await _badgeImageRepository.GetByIdAsync(decodedId);

            if (image?.ImageData is not { Length: > 0 })
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Badge image not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            Response.Headers.CacheControl = "public, max-age=604800";
            return File(image.ImageData, image.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge image {ImageId}", imageId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge image",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
