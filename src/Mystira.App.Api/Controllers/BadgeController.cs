using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;
using YamlDotNet.Serialization;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BadgeController : ControllerBase
{
    private readonly IBadgeConfigurationApiService _badgeService;
    private readonly IMediaApiService _mediaService;
    private readonly ILogger<BadgeController> _logger;

    public BadgeController(
        IBadgeConfigurationApiService badgeService,
        IMediaApiService mediaService,
        ILogger<BadgeController> logger)
    {
        _badgeService = badgeService;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Upload badge configurations from YAML file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<BadgeUploadResult>> UploadBadgeYaml([FromForm] IFormFile yamlFile, [FromForm] bool validateImages = true, [FromForm] bool overwriteExisting = false)
    {
        var result = new BadgeUploadResult { Success = true };

        try
        {
            if (yamlFile == null || yamlFile.Length == 0)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "No YAML file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (!yamlFile.FileName.EndsWith(".yaml") && !yamlFile.FileName.EndsWith(".yml"))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "Please upload a .yaml or .yml file",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Read and parse YAML
            string yamlContent;
            using (var reader = new StreamReader(yamlFile.OpenReadStream()))
            {
                yamlContent = await reader.ReadToEndAsync();
            }

            var deserializer = new DeserializerBuilder()
                .WithCaseInsensitivePropertyMatching()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
                .Build();
            var badgeYaml = deserializer.Deserialize<BadgeYamlFile>(yamlContent);

            if (badgeYaml?.Badges == null || badgeYaml.Badges.Count == 0)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "No badges found in YAML file or invalid format",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Validate image references if requested
            if (validateImages)
            {
                var imageIds = badgeYaml.Badges
                    .Where(b => !string.IsNullOrEmpty(b.ImageId))
                    .Select(b => b.ImageId)
                    .Distinct()
                    .ToList();

                if (imageIds.Count > 0)
                {
                    var validation = await _mediaService.ValidateMediaReferencesAsync(imageIds);
                    if (!validation.IsValid)
                    {
                        result.Errors.Add($"Missing image references: {string.Join(", ", validation.MissingMediaIds)}");
                        if (!overwriteExisting) // If not overwriting, fail completely
                        {
                            result.Success = false;
                            result.Message = "Image validation failed. Please upload missing images first or disable image validation.";
                            return BadRequest(result);
                        }
                    }
                }
            }

            // Process each badge
            foreach (var badgeItem in badgeYaml.Badges)
            {
                try
                {
                    // Check if badge already exists
                    var existingBadge = await _badgeService.GetBadgeConfigurationAsync(badgeItem.Id);
                    if (existingBadge != null && !overwriteExisting)
                    {
                        result.Errors.Add($"Badge '{badgeItem.Id}' already exists (skipped)");
                        result.FailedCount++;
                        continue;
                    }

                    // Note: We don't create the BadgeConfiguration object directly
                    // Instead, we use the API service methods with request objects

                    if (existingBadge != null && overwriteExisting)
                    {
                        // Update existing
                        var updateRequest = new UpdateBadgeConfigurationRequest
                        {
                            Name = badgeItem.Name,
                            Message = badgeItem.Message,
                            Axis = badgeItem.Axis,
                            Threshold = badgeItem.Threshold,
                            ImageId = badgeItem.ImageId
                        };
                        await _badgeService.UpdateBadgeConfigurationAsync(badgeItem.Id, updateRequest);
                    }
                    else
                    {
                        // Create new
                        var createRequest = new CreateBadgeConfigurationRequest
                        {
                            Id = badgeItem.Id,
                            Name = badgeItem.Name,
                            Message = badgeItem.Message,
                            Axis = badgeItem.Axis,
                            Threshold = badgeItem.Threshold,
                            ImageId = badgeItem.ImageId
                        };
                        await _badgeService.CreateBadgeConfigurationAsync(createRequest);
                    }

                    result.SuccessfulUploads.Add(badgeItem.Id);
                    result.UploadedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process badge: {BadgeId}", badgeItem.Id);
                    result.Errors.Add($"Failed to process badge '{badgeItem.Id}': {ex.Message}");
                    result.FailedCount++;
                }
            }

            result.Success = result.FailedCount == 0;
            result.Message = $"Processed {result.UploadedCount} badges successfully, {result.FailedCount} failed";

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading badge YAML file: {FileName}", yamlFile?.FileName);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while uploading badge configuration",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}