using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Polyglot;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Admin endpoints for polyglot persistence management.
/// Provides consistency validation, health checks, sync status, and backfill operations.
/// </summary>
[ApiController]
[Route("api/admin/polyglot")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class PolyglotAdminController : ControllerBase
{
    private readonly IOptions<PolyglotOptions> _options;
    private readonly ILogger<PolyglotAdminController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PolyglotAdminController(
        IOptions<PolyglotOptions> options,
        ILogger<PolyglotAdminController> logger,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Trigger backfill of all entities from Cosmos to PostgreSQL.
    /// Use this before enabling DualWrite mode to sync existing data.
    /// </summary>
    [HttpPost("backfill")]
    public async Task<ActionResult<BackfillSummary>> TriggerBackfill(
        [FromQuery] int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var backfillService = _serviceProvider.GetService<IPolyglotBackfillService>();
        if (backfillService == null)
        {
            return BadRequest(new { error = "Backfill service not available. PostgreSQL may not be configured." });
        }

        _logger.LogInformation("Admin triggered polyglot backfill. BatchSize: {BatchSize}", batchSize);

        var result = await backfillService.BackfillAllAsync(batchSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger backfill for a specific entity type
    /// </summary>
    [HttpPost("backfill/{entityType}")]
    public async Task<ActionResult<BackfillResult>> TriggerEntityBackfill(
        string entityType,
        [FromQuery] int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var backfillService = _serviceProvider.GetService<IPolyglotBackfillService>();
        if (backfillService == null)
        {
            return BadRequest(new { error = "Backfill service not available. PostgreSQL may not be configured." });
        }

        _logger.LogInformation("Admin triggered polyglot backfill for {EntityType}. BatchSize: {BatchSize}", entityType, batchSize);

        BackfillResult result = entityType.ToLower() switch
        {
            "account" or "accounts" => await backfillService.BackfillAccountsAsync(batchSize, cancellationToken),
            "gamesession" or "gamesessions" => await backfillService.BackfillGameSessionsAsync(batchSize, cancellationToken),
            "playerscenariocore" or "playerscenarioscores" => await backfillService.BackfillPlayerScenarioScoresAsync(batchSize, cancellationToken),
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };

        return Ok(result);
    }

    /// <summary>
    /// Get current polyglot persistence configuration and status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<PolyglotStatusResponse> GetStatus()
    {
        var accountRepo = _serviceProvider.GetService<IPolyglotRepository<Account>>();
        var sessionRepo = _serviceProvider.GetService<IPolyglotRepository<GameSession>>();
        var scoreRepo = _serviceProvider.GetService<IPolyglotRepository<PlayerScenarioScore>>();

        return Ok(new PolyglotStatusResponse
        {
            Mode = _options.Value.Mode.ToString(),
            EnableCompensation = _options.Value.EnableCompensation,
            SecondaryWriteTimeoutMs = _options.Value.SecondaryWriteTimeoutMs,
            EnableConsistencyValidation = _options.Value.EnableConsistencyValidation,
            RegisteredRepositories = new[]
            {
                new RepositoryStatus { EntityType = "Account", IsRegistered = accountRepo != null },
                new RepositoryStatus { EntityType = "GameSession", IsRegistered = sessionRepo != null },
                new RepositoryStatus { EntityType = "PlayerScenarioScore", IsRegistered = scoreRepo != null }
            }
        });
    }

    /// <summary>
    /// Check health of primary and secondary backends
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<PolyglotHealthResponse>> GetHealth(CancellationToken cancellationToken)
    {
        var accountRepo = _serviceProvider.GetService<IPolyglotRepository<Account>>();

        if (accountRepo == null)
        {
            return Ok(new PolyglotHealthResponse
            {
                PrimaryHealthy = true, // Cosmos is always available if app is running
                SecondaryHealthy = false,
                SecondaryConfigured = false,
                Message = "PostgreSQL not configured - running in SingleStore mode"
            });
        }

        var primaryHealthy = await accountRepo.IsPrimaryHealthyAsync(cancellationToken);
        var secondaryHealthy = await accountRepo.IsSecondaryHealthyAsync(cancellationToken);

        return Ok(new PolyglotHealthResponse
        {
            PrimaryHealthy = primaryHealthy,
            SecondaryHealthy = secondaryHealthy,
            SecondaryConfigured = true,
            Message = secondaryHealthy
                ? "Both backends healthy"
                : "Secondary backend (PostgreSQL) is not healthy"
        });
    }

    /// <summary>
    /// Validate consistency for a specific entity between backends
    /// </summary>
    [HttpGet("consistency/{entityType}/{entityId}")]
    public async Task<ActionResult<ConsistencyResult>> ValidateConsistency(
        string entityType,
        string entityId,
        CancellationToken cancellationToken)
    {
        IPolyglotRepository<Account>? repo = entityType.ToLower() switch
        {
            "account" => _serviceProvider.GetService<IPolyglotRepository<Account>>(),
            _ => null
        };

        if (repo == null)
        {
            // Try other entity types
            if (entityType.ToLower() == "gamesession")
            {
                var sessionRepo = _serviceProvider.GetService<IPolyglotRepository<GameSession>>();
                if (sessionRepo != null)
                {
                    return Ok(await sessionRepo.ValidateConsistencyAsync(entityId, cancellationToken));
                }
            }
            else if (entityType.ToLower() == "playerscenariocore")
            {
                var scoreRepo = _serviceProvider.GetService<IPolyglotRepository<PlayerScenarioScore>>();
                if (scoreRepo != null)
                {
                    return Ok(await scoreRepo.ValidateConsistencyAsync(entityId, cancellationToken));
                }
            }

            return NotFound(new { error = $"No polyglot repository found for entity type: {entityType}" });
        }

        var result = await repo.ValidateConsistencyAsync(entityId, cancellationToken);
        return Ok(result);
    }
}

public class PolyglotStatusResponse
{
    public string Mode { get; set; } = string.Empty;
    public bool EnableCompensation { get; set; }
    public int SecondaryWriteTimeoutMs { get; set; }
    public bool EnableConsistencyValidation { get; set; }
    public RepositoryStatus[] RegisteredRepositories { get; set; } = Array.Empty<RepositoryStatus>();
}

public class RepositoryStatus
{
    public string EntityType { get; set; } = string.Empty;
    public bool IsRegistered { get; set; }
}

public class PolyglotHealthResponse
{
    public bool PrimaryHealthy { get; set; }
    public bool SecondaryHealthy { get; set; }
    public bool SecondaryConfigured { get; set; }
    public string Message { get; set; } = string.Empty;
}
