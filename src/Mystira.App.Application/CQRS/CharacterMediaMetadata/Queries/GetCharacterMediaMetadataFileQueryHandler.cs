using Microsoft.Extensions.Logging;
using Mystira.App.Api.Models;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;

/// <summary>
/// Handler for retrieving the character media metadata file.
/// Returns the singleton file containing all character media metadata entries.
/// </summary>
public class GetCharacterMediaMetadataFileQueryHandler
    : IQueryHandler<GetCharacterMediaMetadataFileQuery, CharacterMediaMetadataFile?>
{
    private readonly ICharacterMediaMetadataFileRepository _repository;
    private readonly ILogger<GetCharacterMediaMetadataFileQueryHandler> _logger;

    public GetCharacterMediaMetadataFileQueryHandler(
        ICharacterMediaMetadataFileRepository repository,
        ILogger<GetCharacterMediaMetadataFileQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CharacterMediaMetadataFile?> Handle(
        GetCharacterMediaMetadataFileQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting character media metadata file");

        var metadataFile = await _repository.GetAsync();

        if (metadataFile == null)
        {
            _logger.LogWarning("Character media metadata file not found");
            return null;
        }

        _logger.LogInformation("Found character media metadata file with {Count} entries", metadataFile.Entries.Count);

        // Domain model matches API model structure, can return directly
        return new CharacterMediaMetadataFile
        {
            Id = metadataFile.Id,
            Entries = metadataFile.Entries.Select(e => new CharacterMediaMetadataEntry
            {
                Id = e.Id,
                Title = e.Title,
                FileName = e.FileName,
                Type = e.Type,
                Description = e.Description,
                AgeRating = e.AgeRating,
                Tags = e.Tags,
                Loopable = e.Loopable
            }).ToList(),
            CreatedAt = metadataFile.CreatedAt,
            UpdatedAt = metadataFile.UpdatedAt,
            Version = metadataFile.Version
        };
    }
}
