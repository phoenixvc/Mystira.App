using Wolverine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Badges.Queries;
using Mystira.Contracts.App.Responses.Common;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/badges/images")]
public class BadgeImagesController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<BadgeImagesController> _logger;

    public BadgeImagesController(IMessageBus bus, ILogger<BadgeImagesController> logger)
    {
        _bus = bus;
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
            var result = await _bus.InvokeAsync<BadgeImageResult?>(new GetBadgeImageQuery(imageId));

            if (result is null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Badge image not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            Response.Headers.CacheControl = "public, max-age=604800";
            return File(result.ImageData, result.ContentType);
        }
        catch (Exception ex) when (
            ex is not StackOverflowException &&
            ex is not OutOfMemoryException &&
            ex is not ThreadAbortException &&
            ex is not AccessViolationException &&
            ex is not AppDomainUnloadedException &&
            ex is not BadImageFormatException &&
            ex is not CannotUnloadAppDomainException
        )
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
