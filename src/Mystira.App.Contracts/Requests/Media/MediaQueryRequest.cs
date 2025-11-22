namespace Mystira.App.Contracts.Requests.Media;

/// <summary>
/// Request model for querying media assets
/// </summary>
public class MediaQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? MediaType { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

