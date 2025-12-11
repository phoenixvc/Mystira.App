using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;
using Mystira.App.Infrastructure.Azure.Services;
using Xunit;

namespace Mystira.App.Api.Tests.Services;

public class MediaApiServiceTests
{
    [Fact]
    public async Task UploadMediaAsync_ConvertsWhatsappAudioBeforeSaving()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MystiraAppDbContext>()
            .UseInMemoryDatabase(databaseName: $"media_api_{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new MystiraAppDbContext(options);

        var blobServiceMock = new Mock<IAzureBlobService>();
        blobServiceMock
            .Setup(b => b.UploadMediaAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://cdn.example.com/audio/voice_note.mp3");

        var metadataServiceMock = new Mock<IMediaMetadataService>();
        metadataServiceMock
            .Setup(m => m.GetMediaMetadataFileAsync())
            .ReturnsAsync(new MediaMetadataFile
            {
                Entries =
                {
                    new MediaMetadataEntry
                    {
                        Id = "voice-note",
                        FileName = "voice_note.mp3",
                        Type = "audio",
                        Title = "Voice Note"
                    }
                }
            });

        var audioOutput = Encoding.UTF8.GetBytes("converted-audio");
        var audioTranscoderMock = new Mock<IAudioTranscodingService>();
        audioTranscoderMock
            .Setup(t => t.ConvertWhatsAppVoiceNoteAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioTranscodingResult(new MemoryStream(audioOutput), "voice_note.mp3", "audio/mpeg"));

        var loggerMock = new Mock<ILogger<MediaApiService>>();

        var mediaService = new MediaApiService(dbContext, blobServiceMock.Object, metadataServiceMock.Object, loggerMock.Object, audioTranscoderMock.Object);

        var sourceBytes = Encoding.UTF8.GetBytes("original-whatsapp-audio");
        await using var sourceStream = new MemoryStream(sourceBytes);
        var formFile = new FormFile(sourceStream, 0, sourceBytes.Length, "file", "voice_note.waptt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/ogg"
        };

        // Act
        var result = await mediaService.UploadMediaAsync(formFile, "voice-note", "audio");

        // Assert
        Assert.Equal("audio/mpeg", result.MimeType);
        Assert.Equal("voice-note", result.MediaId);
        Assert.Equal(audioOutput.Length, result.FileSizeBytes);

        audioTranscoderMock.Verify(t => t.ConvertWhatsAppVoiceNoteAsync(It.IsAny<Stream>(), "voice_note.waptt", It.IsAny<CancellationToken>()), Times.Once);
        blobServiceMock.Verify(b => b.UploadMediaAsync(It.IsAny<Stream>(), "voice_note.mp3", "audio/mpeg"), Times.Once);

        var savedAsset = await dbContext.MediaAssets.SingleAsync(m => m.MediaId == "voice-note");
        Assert.Equal("audio/mpeg", savedAsset.MimeType);
        Assert.Equal(audioOutput.Length, savedAsset.FileSizeBytes);
    }
}
