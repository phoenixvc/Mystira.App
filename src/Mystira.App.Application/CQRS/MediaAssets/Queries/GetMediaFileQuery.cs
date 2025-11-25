using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Query to retrieve the actual media file content for download/streaming.
/// Returns the file stream, content type, and filename.
/// Not cached as file downloads are transient operations.
/// </summary>
public record GetMediaFileQuery(string MediaId)
    : IQuery<(Stream stream, string contentType, string fileName)?>;
