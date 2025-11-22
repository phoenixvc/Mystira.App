using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Models.Parsers;

/// <summary>
/// Parser for converting media references dictionary data to MediaReferences domain object
/// </summary>
public static class MediaReferencesParser
{
    public static MediaReferences Parse(IDictionary<object, object> mediaDict)
    {
        var media = new MediaReferences();

        // Check for Image field with various naming conventions
        if (mediaDict.TryGetValue("image", out var imageObj) ||
            mediaDict.TryGetValue("Image", out imageObj))
        {
            media.Image = imageObj?.ToString();
        }

        // Check for Audio field with various naming conventions
        if (mediaDict.TryGetValue("audio", out var audioObj) ||
            mediaDict.TryGetValue("Audio", out audioObj))
        {
            media.Audio = audioObj?.ToString();
        }

        // Check for Video field with various naming conventions
        if (mediaDict.TryGetValue("video", out var videoObj) ||
            mediaDict.TryGetValue("Video", out videoObj))
        {
            media.Video = videoObj?.ToString();
        }

        return media;
    }
}

