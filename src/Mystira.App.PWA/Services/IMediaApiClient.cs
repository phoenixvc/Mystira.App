namespace Mystira.App.PWA.Services;

public interface IMediaApiClient
{
    Task<string?> GetMediaUrlFromId(string mediaId);
    string GetMediaResourceEndpointUrl(string mediaId);
}

