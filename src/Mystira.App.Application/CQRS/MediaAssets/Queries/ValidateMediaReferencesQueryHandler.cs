using MediatR;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Handler for validating media references exist in the system.
/// Ensures data integrity when creating content that references media assets.
/// </summary>
public class ValidateMediaReferencesQueryHandler
    : IQueryHandler<ValidateMediaReferencesQuery, MediaValidationResult>
{
    private readonly IMediaAssetRepository _repository;
    private readonly ILogger<ValidateMediaReferencesQueryHandler> _logger;

    public ValidateMediaReferencesQueryHandler(
        IMediaAssetRepository repository,
        ILogger<ValidateMediaReferencesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MediaValidationResult> Handle(
        ValidateMediaReferencesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Validating {Count} media references",
            request.MediaIds.Count);

        var result = new MediaValidationResult
        {
            TotalValidated = request.MediaIds.Count
        };

        if (request.MediaIds.Count == 0)
        {
            return result; // Empty list is valid
        }

        // Remove duplicates
        var uniqueMediaIds = request.MediaIds.Distinct().ToList();

        // Check each media ID
        var missingIds = new List<string>();

        foreach (var mediaId in uniqueMediaIds)
        {
            var exists = await _repository.ExistsAsync(mediaId);
            if (!exists)
            {
                missingIds.Add(mediaId);
            }
        }

        result.MissingMediaIds = missingIds;
        result.ValidCount = uniqueMediaIds.Count - missingIds.Count;

        if (missingIds.Any())
        {
            _logger.LogWarning(
                "Media validation failed: {MissingCount} missing media IDs: {MissingIds}",
                missingIds.Count,
                string.Join(", ", missingIds));
        }
        else
        {
            _logger.LogInformation(
                "Media validation successful: all {Count} references valid",
                uniqueMediaIds.Count);
        }

        return result;
    }
}
