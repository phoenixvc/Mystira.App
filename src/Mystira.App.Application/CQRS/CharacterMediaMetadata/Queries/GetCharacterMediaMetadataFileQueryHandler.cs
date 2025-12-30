using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;

/// <summary>
/// Wolverine handler for retrieving the character media metadata file.
/// Returns the singleton file containing all character media metadata entries.
/// </summary>
public static class GetCharacterMediaMetadataFileQueryHandler
{
    public static async Task<CharacterMediaMetadataFile?> Handle(
        GetCharacterMediaMetadataFileQuery request,
        ICharacterMediaMetadataFileRepository repository,
        ILogger<GetCharacterMediaMetadataFileQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting character media metadata file");

        var metadataFile = await repository.GetAsync();

        if (metadataFile == null)
        {
            logger.LogWarning("Character media metadata file not found");
            return null;
        }

        logger.LogInformation("Found character media metadata file with {Count} entries", metadataFile.Entries.Count);

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
