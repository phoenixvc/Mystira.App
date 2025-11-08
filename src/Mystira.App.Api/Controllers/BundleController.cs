using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Admin only
public class BundleController : ControllerBase
{
    private readonly IBundleService _bundleService;
    private readonly ILogger<BundleController> _logger;

    public BundleController(IBundleService bundleService, ILogger<BundleController> logger)
    {
        _bundleService = bundleService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a bundle file
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<BundleValidationResult>> ValidateBundle([FromForm] IFormFile bundleFile)
    {
        try
        {
            if (bundleFile == null || bundleFile.Length == 0)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "No bundle file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _bundleService.ValidateBundleAsync(bundleFile);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bundle file: {FileName}", bundleFile?.FileName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while validating bundle",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Uploads and processes a bundle file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<BundleUploadResult>> UploadBundle([FromForm] IFormFile bundleFile, [FromForm] bool validateReferences = true, [FromForm] bool overwriteExisting = false)
    {
        try
        {
            if (bundleFile == null || bundleFile.Length == 0)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "No bundle file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var request = new BundleUploadRequest
            {
                ValidateReferences = validateReferences,
                OverwriteExisting = overwriteExisting
            };

            var result = await _bundleService.UploadBundleAsync(bundleFile, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading bundle file: {FileName}", bundleFile?.FileName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while uploading bundle",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}