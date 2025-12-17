using FluentAssertions;
using Mystira.App.Shared.Media;
using Xunit;

namespace Mystira.App.Application.Tests.Shared;

public class MimeTypeRegistryTests
{
    #region GetMimeType Tests

    [Theory]
    [InlineData(".mp3", "audio/mpeg")]
    [InlineData(".wav", "audio/wav")]
    [InlineData(".ogg", "audio/ogg")]
    [InlineData(".aac", "audio/aac")]
    [InlineData(".m4a", "audio/mp4")]
    public void GetMimeType_WithAudioExtension_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(extension);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".mp4", "video/mp4")]
    [InlineData(".avi", "video/x-msvideo")]
    [InlineData(".mov", "video/quicktime")]
    [InlineData(".wmv", "video/x-ms-wmv")]
    [InlineData(".mkv", "video/x-matroska")]
    public void GetMimeType_WithVideoExtension_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(extension);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".webp", "image/webp")]
    public void GetMimeType_WithImageExtension_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(extension);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData("test.mp3", "audio/mpeg")]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("image.png", "image/png")]
    [InlineData("path/to/file.wav", "audio/wav")]
    public void GetMimeType_WithFullFilename_ReturnsCorrectMimeType(string filename, string expectedMimeType)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(filename);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".MP3", "audio/mpeg")]
    [InlineData(".Wav", "audio/wav")]
    [InlineData(".PNG", "image/png")]
    public void GetMimeType_WithMixedCaseExtension_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(extension);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".xyz")]
    [InlineData(".unknown")]
    [InlineData(".doc")]
    public void GetMimeType_WithUnknownExtension_ReturnsOctetStream(string extension)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(extension);

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMimeType_WithNullOrWhitespace_ReturnsOctetStream(string? input)
    {
        // Act
        var result = MimeTypeRegistry.GetMimeType(input!);

        // Assert
        result.Should().Be("application/octet-stream");
    }

    #endregion

    #region GetExtension Tests

    [Theory]
    [InlineData("audio/mpeg", ".mp3")]
    [InlineData("video/mp4", ".mp4")]
    [InlineData("image/png", ".png")]
    public void GetExtension_WithKnownMimeType_ReturnsExtension(string mimeType, string expectedExtension)
    {
        // Act
        var result = MimeTypeRegistry.GetExtension(mimeType);

        // Assert
        result.Should().Be(expectedExtension);
    }

    [Theory]
    [InlineData("application/unknown")]
    [InlineData("text/plain")]
    public void GetExtension_WithUnknownMimeType_ReturnsNull(string mimeType)
    {
        // Act
        var result = MimeTypeRegistry.GetExtension(mimeType);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetMediaType Tests

    [Theory]
    [InlineData(".mp3", "audio")]
    [InlineData(".wav", "audio")]
    [InlineData(".ogg", "audio")]
    public void GetMediaType_WithAudioExtension_ReturnsAudio(string extension, string expectedType)
    {
        // Act
        var result = MimeTypeRegistry.GetMediaType(extension);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData(".mp4", "video")]
    [InlineData(".avi", "video")]
    [InlineData(".mov", "video")]
    public void GetMediaType_WithVideoExtension_ReturnsVideo(string extension, string expectedType)
    {
        // Act
        var result = MimeTypeRegistry.GetMediaType(extension);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData(".jpg", "image")]
    [InlineData(".png", "image")]
    [InlineData(".gif", "image")]
    public void GetMediaType_WithImageExtension_ReturnsImage(string extension, string expectedType)
    {
        // Act
        var result = MimeTypeRegistry.GetMediaType(extension);

        // Assert
        result.Should().Be(expectedType);
    }

    [Fact]
    public void GetMediaType_WithUnknownExtension_ReturnsNull()
    {
        // Act
        var result = MimeTypeRegistry.GetMediaType(".xyz");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllowedExtensions Tests

    [Fact]
    public void GetAllowedExtensions_ForAudio_ReturnsAudioExtensions()
    {
        // Act
        var result = MimeTypeRegistry.GetAllowedExtensions("audio");

        // Assert
        result.Should().Contain(".mp3");
        result.Should().Contain(".wav");
        result.Should().Contain(".ogg");
    }

    [Fact]
    public void GetAllowedExtensions_ForVideo_ReturnsVideoExtensions()
    {
        // Act
        var result = MimeTypeRegistry.GetAllowedExtensions("video");

        // Assert
        result.Should().Contain(".mp4");
        result.Should().Contain(".avi");
        result.Should().Contain(".mov");
    }

    [Fact]
    public void GetAllowedExtensions_ForImage_ReturnsImageExtensions()
    {
        // Act
        var result = MimeTypeRegistry.GetAllowedExtensions("image");

        // Assert
        result.Should().Contain(".jpg");
        result.Should().Contain(".png");
        result.Should().Contain(".gif");
    }

    [Theory]
    [InlineData("AUDIO")]
    [InlineData("Audio")]
    [InlineData("AuDiO")]
    public void GetAllowedExtensions_IsCaseInsensitive(string mediaType)
    {
        // Act
        var result = MimeTypeRegistry.GetAllowedExtensions(mediaType);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(".mp3");
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData(null)]
    public void GetAllowedExtensions_WithUnknownType_ReturnsEmptyArray(string? mediaType)
    {
        // Act
        var result = MimeTypeRegistry.GetAllowedExtensions(mediaType!);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region IsValidExtension Tests

    [Theory]
    [InlineData(".mp3", "audio", true)]
    [InlineData(".wav", "audio", true)]
    [InlineData(".mp4", "video", true)]
    [InlineData(".png", "image", true)]
    [InlineData(".mp3", "video", false)]
    [InlineData(".mp4", "audio", false)]
    [InlineData(".png", "audio", false)]
    public void IsValidExtension_ValidatesCorrectly(string extension, string mediaType, bool expected)
    {
        // Act
        var result = MimeTypeRegistry.IsValidExtension(extension, mediaType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("song.mp3", "audio", true)]
    [InlineData("video.mp4", "video", true)]
    [InlineData("photo.png", "image", true)]
    public void IsValidExtension_WithFullFilename_ValidatesCorrectly(string filename, string mediaType, bool expected)
    {
        // Act
        var result = MimeTypeRegistry.IsValidExtension(filename, mediaType);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetMaxFileSizeBytes Tests

    [Theory]
    [InlineData("audio", 50 * 1024 * 1024)]   // 50MB
    [InlineData("video", 100 * 1024 * 1024)]  // 100MB
    [InlineData("image", 10 * 1024 * 1024)]   // 10MB
    public void GetMaxFileSizeBytes_ReturnsCorrectLimits(string mediaType, long expectedSize)
    {
        // Act
        var result = MimeTypeRegistry.GetMaxFileSizeBytes(mediaType);

        // Assert
        result.Should().Be(expectedSize);
    }

    [Fact]
    public void GetMaxFileSizeBytes_WithUnknownType_ReturnsDefaultLimit()
    {
        // Act
        var result = MimeTypeRegistry.GetMaxFileSizeBytes("unknown");

        // Assert
        result.Should().Be(10 * 1024 * 1024); // Default 10MB
    }

    #endregion

    #region AllTypes Property Tests

    [Fact]
    public void AllTypes_ContainsAllAudioTypes()
    {
        // Act & Assert
        foreach (var audioType in MimeTypeRegistry.AudioTypes)
        {
            MimeTypeRegistry.AllTypes.Should().ContainKey(audioType.Key);
            MimeTypeRegistry.AllTypes[audioType.Key].Should().Be(audioType.Value);
        }
    }

    [Fact]
    public void AllTypes_ContainsAllVideoTypes()
    {
        // Act & Assert
        foreach (var videoType in MimeTypeRegistry.VideoTypes)
        {
            MimeTypeRegistry.AllTypes.Should().ContainKey(videoType.Key);
            MimeTypeRegistry.AllTypes[videoType.Key].Should().Be(videoType.Value);
        }
    }

    [Fact]
    public void AllTypes_ContainsAllImageTypes()
    {
        // Act & Assert
        foreach (var imageType in MimeTypeRegistry.ImageTypes)
        {
            MimeTypeRegistry.AllTypes.Should().ContainKey(imageType.Key);
            MimeTypeRegistry.AllTypes[imageType.Key].Should().Be(imageType.Value);
        }
    }

    #endregion
}
