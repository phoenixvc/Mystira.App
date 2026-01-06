using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/badges/images")]
public class BadgeImagesController : ControllerBase
{
    private readonly IBadgeAdminService _badgeAdminService;
    private readonly ILogger<BadgeImagesController> _logger;

    public BadgeImagesController(IBadgeAdminService badgeAdminService, ILogger<BadgeImagesController> logger)
    {
        _badgeAdminService = badgeAdminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BadgeImageDto>>> SearchImages([FromQuery] string? imageId)
    {
        var images = await _badgeAdminService.SearchImagesAsync(imageId, includeData: true);
        return Ok(images);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BadgeImageDto>> GetImage(string id)
    {
        var image = await _badgeAdminService.GetImageAsync(id, includeData: true);
        if (image == null)
        {
            return NotFound(new { message = "Badge image not found" });
        }

        return Ok(image);
    }

    [HttpPost]
    public async Task<ActionResult<BadgeImageDto>> UploadImage([FromForm] BadgeImageUploadRequest request)
    {
        if (!ModelState.IsValid || request.File == null)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var image = await _badgeAdminService.UploadImageAsync(request.ImageId, request.File);
            return CreatedAtAction(nameof(GetImage), new { id = image.Id }, image);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Badge image upload failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(string id)
    {
        var deleted = await _badgeAdminService.DeleteImageAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Badge image not found" });
        }

        return NoContent();
    }
}
