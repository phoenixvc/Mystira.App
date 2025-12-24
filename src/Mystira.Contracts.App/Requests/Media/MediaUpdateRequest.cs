namespace Mystira.Contracts.App.Requests.Media;

/// <summary>
/// Request model for updating media metadata
/// </summary>
public class MediaUpdateRequest
{
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string? MediaType { get; set; }
}

