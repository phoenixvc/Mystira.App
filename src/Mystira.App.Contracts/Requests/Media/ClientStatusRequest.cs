namespace Mystira.App.Contracts.Requests.Media;

/// <summary>
/// Represents a request to retrieve client status information, including client and content version details.
/// </summary>
public class ClientStatusRequest
{
    public string ClientVersion { get; set; } = string.Empty;
    public string ContentVersion { get; set; } = string.Empty;
}

