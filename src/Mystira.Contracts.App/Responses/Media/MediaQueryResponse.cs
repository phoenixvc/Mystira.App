using Mystira.App.Domain.Models;

namespace Mystira.Contracts.App.Responses.Media;

/// <summary>
/// Response model for media queries
/// </summary>
public class MediaQueryResponse
{
    public List<MediaAsset> Media { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

