using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.MediaMetadata.Queries;

/// <summary>
/// Handler for retrieving the media metadata file.
/// Returns the singleton media metadata file containing all media entries.
/// </summary>
public class GetMediaMetadataFileQueryHandler : IQueryHandler<GetMediaMetadataFileQuery, MediaMetadataFile?>
{
    private readonly IMediaMetadataFileRepository _repository;
    private readonly ILogger<GetMediaMetadataFileQueryHandler> _logger;

    public GetMediaMetadataFileQueryHandler(
        IMediaMetadataFileRepository repository,
        ILogger<GetMediaMetadataFileQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MediaMetadataFile?> Handle(
        GetMediaMetadataFileQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting media metadata file");

        var mediaMetadataFile = await _repository.GetAsync();

        if (mediaMetadataFile == null)
        {
            _logger.LogWarning("Media metadata file not found");
            return null;
        }

        _logger.LogInformation("Found media metadata file with {Count} entries", mediaMetadataFile.Entries.Count);

        // Domain model matches API model structure, can return directly
        return new MediaMetadataFile
        {
            Id = mediaMetadataFile.Id,
            Entries = mediaMetadataFile.Entries.Select(e => new MediaMetadataEntry
            {
                Id = e.Id,
                Title = e.Title,
                FileName = e.FileName,
                Type = e.Type,
                Description = e.Description,
                AgeRating = e.AgeRating,
                SubjectReferenceId = e.SubjectReferenceId,
                ClassificationTags = e.ClassificationTags.Select(ct => new ClassificationTag
                {
                    Key = ct.Key,
                    Value = ct.Value
                }).ToList(),
                Modifiers = e.Modifiers.Select(m => new Modifier
                {
                    Key = m.Key,
                    Value = m.Value
                }).ToList(),
                Loopable = e.Loopable
            }).ToList(),
            CreatedAt = mediaMetadataFile.CreatedAt,
            UpdatedAt = mediaMetadataFile.UpdatedAt,
            Version = mediaMetadataFile.Version
        };
    }
}
