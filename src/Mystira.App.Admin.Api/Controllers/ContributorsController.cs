using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.UseCases.Contributors;
using Mystira.App.Contracts.Requests.Contributors;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Contracts.Responses.Contributors;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

/// <summary>
/// Controller for managing contributors and Story Protocol royalty splits
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize]
public class ContributorsController : ControllerBase
{
    private readonly SetScenarioContributorsUseCase _setScenarioContributorsUseCase;
    private readonly SetBundleContributorsUseCase _setBundleContributorsUseCase;
    private readonly RegisterScenarioIpAssetUseCase _registerScenarioIpAssetUseCase;
    private readonly RegisterBundleIpAssetUseCase _registerBundleIpAssetUseCase;
    private readonly ILogger<ContributorsController> _logger;

    public ContributorsController(
        SetScenarioContributorsUseCase setScenarioContributorsUseCase,
        SetBundleContributorsUseCase setBundleContributorsUseCase,
        RegisterScenarioIpAssetUseCase registerScenarioIpAssetUseCase,
        RegisterBundleIpAssetUseCase registerBundleIpAssetUseCase,
        ILogger<ContributorsController> logger)
    {
        _setScenarioContributorsUseCase = setScenarioContributorsUseCase;
        _setBundleContributorsUseCase = setBundleContributorsUseCase;
        _registerScenarioIpAssetUseCase = registerScenarioIpAssetUseCase;
        _registerBundleIpAssetUseCase = registerBundleIpAssetUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Set contributors for a scenario
    /// </summary>
    /// <param name="scenarioId">ID of the scenario</param>
    /// <param name="request">List of contributors with royalty splits</param>
    [HttpPost("scenarios/{scenarioId}")]
    public async Task<ActionResult<StoryProtocolResponse>> SetScenarioContributors(
        string scenarioId,
        [FromBody] SetContributorsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var metadata = await _setScenarioContributorsUseCase.ExecuteAsync(scenarioId, request);
            var response = MapToResponse(metadata);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error setting contributors for scenario {ScenarioId}", scenarioId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting contributors for scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while setting contributors",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Set contributors for a content bundle
    /// </summary>
    /// <param name="bundleId">ID of the bundle</param>
    /// <param name="request">List of contributors with royalty splits</param>
    [HttpPost("bundles/{bundleId}")]
    public async Task<ActionResult<StoryProtocolResponse>> SetBundleContributors(
        string bundleId,
        [FromBody] SetContributorsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var metadata = await _setBundleContributorsUseCase.ExecuteAsync(bundleId, request);
            var response = MapToResponse(metadata);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error setting contributors for bundle {BundleId}", bundleId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting contributors for bundle {BundleId}", bundleId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while setting contributors",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Register a scenario as an IP Asset on Story Protocol
    /// </summary>
    /// <param name="scenarioId">ID of the scenario</param>
    /// <param name="request">Registration parameters</param>
    [HttpPost("scenarios/{scenarioId}/register")]
    public async Task<ActionResult<StoryProtocolResponse>> RegisterScenarioIpAsset(
        string scenarioId,
        [FromBody] RegisterIpAssetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var metadata = await _registerScenarioIpAssetUseCase.ExecuteAsync(scenarioId, request);
            var response = MapToResponse(metadata);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error registering scenario {ScenarioId}", scenarioId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation registering scenario {ScenarioId}", scenarioId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering scenario {ScenarioId}", scenarioId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while registering IP asset",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Register a bundle as an IP Asset on Story Protocol
    /// </summary>
    /// <param name="bundleId">ID of the bundle</param>
    /// <param name="request">Registration parameters</param>
    [HttpPost("bundles/{bundleId}/register")]
    public async Task<ActionResult<StoryProtocolResponse>> RegisterBundleIpAsset(
        string bundleId,
        [FromBody] RegisterIpAssetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var metadata = await _registerBundleIpAssetUseCase.ExecuteAsync(bundleId, request);
            var response = MapToResponse(metadata);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error registering bundle {BundleId}", bundleId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation registering bundle {BundleId}", bundleId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering bundle {BundleId}", bundleId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while registering IP asset",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Maps domain StoryProtocolMetadata to API response
    /// </summary>
    private StoryProtocolResponse MapToResponse(StoryProtocolMetadata metadata)
    {
        return new StoryProtocolResponse
        {
            IpAssetId = metadata.IpAssetId,
            RegistrationTxHash = metadata.RegistrationTxHash,
            RegisteredAt = metadata.RegisteredAt,
            RoyaltyModuleId = metadata.RoyaltyModuleId,
            IsRegistered = metadata.IsRegistered,
            Contributors = metadata.Contributors.Select(c => new ContributorResponse
            {
                Id = c.Id,
                Name = c.Name,
                WalletAddress = c.WalletAddress,
                Role = c.Role,
                ContributionPercentage = c.ContributionPercentage,
                Email = c.Email,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt
            }).ToList(),
            ContributorCount = metadata.ContributorCount,
            TotalPercentage = metadata.Contributors.Sum(c => c.ContributionPercentage)
        };
    }
}
