using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;

/// <summary>
/// Wolverine handler for retrieving a specific character media metadata entry by ID.
/// Searches through the metadata file to find the requested entry.
/// </summary>
public static class GetCharacterMediaMetadataEntryQueryHandler
{
    public static async Task<CharacterMediaMetadataEntry?> Handle(
        GetCharacterMediaMetadataEntryQuery request,
        ICharacterMediaMetadataFileRepository repository,
        ILogger<GetCharacterMediaMetadataEntryQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Getting character media metadata entry: {EntryId}", request.EntryId);

        var metadataFile = await repository.GetAsync();

        if (metadataFile == null)
        {
            logger.LogWarning("Character media metadata file not found");
            return null;
        }

        var domainEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == request.EntryId);

        if (domainEntry == null)
        {
            logger.LogWarning("Character media metadata entry not found: {EntryId}", request.EntryId);
            return null;
        }

        logger.LogInformation("Found character media metadata entry: {Title}", domainEntry.Title);

        // Map from Domain to API model
        return new CharacterMediaMetadataEntry
        {
            Id = domainEntry.Id,
            Title = domainEntry.Title,
            FileName = domainEntry.FileName,
            Type = domainEntry.Type,
            Description = domainEntry.Description,
            AgeRating = domainEntry.AgeRating,
            Tags = domainEntry.Tags,
            Loopable = domainEntry.Loopable
        };
    }
}
