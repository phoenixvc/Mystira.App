using Mystira.App.Domain.Models;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.CharacterMediaMetadata.Queries;

/// <summary>
/// Query to get the character media metadata file.
/// </summary>
public record GetCharacterMediaMetadataFileQuery : IQuery<CharacterMediaMetadataFile?>;
