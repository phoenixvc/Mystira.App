using Mystira.App.Domain.Models;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.MediaMetadata.Queries;

/// <summary>
/// Query to get the media metadata file.
/// </summary>
public record GetMediaMetadataFileQuery : IQuery<MediaMetadataFile?>;
