using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;

/// <summary>
/// Handler for retrieving a specific character media metadata entry by ID.
/// Searches through the metadata file to find the requested entry.
/// </summary>
public class GetCharacterMediaMetadataEntryQueryHandler
    : IQueryHandler<GetCharacterMediaMetadataEntryQuery, CharacterMediaMetadataEntry?>
{
    private readonly ICharacterMediaMetadataFileRepository _repository;
    private readonly ILogger<GetCharacterMediaMetadataEntryQueryHandler> _logger;

    public GetCharacterMediaMetadataEntryQueryHandler(
        ICharacterMediaMetadataFileRepository repository,
        ILogger<GetCharacterMediaMetadataEntryQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CharacterMediaMetadataEntry?> Handle(
        GetCharacterMediaMetadataEntryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting character media metadata entry: {EntryId}", request.EntryId);

        var metadataFile = await _repository.GetAsync();

        if (metadataFile == null)
        {
            _logger.LogWarning("Character media metadata file not found");
            return null;
        }

        var domainEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == request.EntryId);

        if (domainEntry == null)
        {
            _logger.LogWarning("Character media metadata entry not found: {EntryId}", request.EntryId);
            return null;
        }

        _logger.LogInformation("Found character media metadata entry: {Title}", domainEntry.Title);

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
