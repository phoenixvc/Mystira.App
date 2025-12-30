using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.MediaMetadata.Queries;

/// <summary>
/// Query to get the media metadata file.
/// </summary>
public record GetMediaMetadataFileQuery : IQuery<MediaMetadataFile?>;
