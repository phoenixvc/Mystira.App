using Mystira.App.Api.Models;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;

/// <summary>
/// Query to get a specific character media metadata entry by ID.
/// </summary>
public record GetCharacterMediaMetadataEntryQuery(string EntryId) : IQuery<CharacterMediaMetadataEntry?>;
