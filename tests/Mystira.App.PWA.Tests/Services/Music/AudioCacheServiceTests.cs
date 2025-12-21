using Moq;
using Mystira.App.PWA.Models;
using Mystira.App.Domain.Models;
using Mystira.App.PWA.Services;
using Mystira.App.PWA.Services.Music;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Net;
using Moq.Protected;
using Scene = Mystira.App.PWA.Models.Scene;
using Scenario = Mystira.App.PWA.Models.Scenario;

namespace Mystira.App.PWA.Tests.Services.Music;

public class AudioCacheServiceTests
{
    private readonly Mock<IMediaApiClient> _mediaApiClientMock;
    private readonly Mock<ILogger<AudioCacheService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly AudioCacheService _sut;

    public AudioCacheServiceTests()
    {
        _mediaApiClientMock = new Mock<IMediaApiClient>();
        _loggerMock = new Mock<ILogger<AudioCacheService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _sut = new AudioCacheService(_loggerMock.Object, _mediaApiClientMock.Object, _httpClient);
    }

    [Fact]
    public async Task CacheScenarioAudioAsync_ShouldFetchUniqueAudioTracks()
    {
        // Arrange
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Media = new SceneMedia { Audio = "audio1" },
                    SoundEffects = new List<Mystira.App.Domain.Models.SceneSoundEffect>
                    {
                        new Mystira.App.Domain.Models.SceneSoundEffect { Track = "sfx1" }
                    }
                },
                new Scene
                {
                    Media = new SceneMedia { Audio = "audio1" }, // Duplicate
                    SoundEffects = new List<Mystira.App.Domain.Models.SceneSoundEffect>
                    {
                        new Mystira.App.Domain.Models.SceneSoundEffect { Track = "sfx2" }
                    }
                }
            },
            MusicPalette = new Mystira.App.Domain.Models.MusicPalette
            {
                TracksByProfile = new Dictionary<string, List<string>>
                {
                    { "Neutral", new List<string> { "music1", "audio1" } }
                }
            }
        };

        _mediaApiClientMock.Setup(x => x.GetMediaResourceEndpointUrl(It.IsAny<string>()))
            .Returns((string id) => $"https://api.example.com/media/{id}");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        await _sut.CacheScenarioAudioAsync(scenario);

        // Assert
        // audio1, sfx1, sfx2, music1 -> 4 unique tracks
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(4),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );

        _mediaApiClientMock.Verify(x => x.GetMediaResourceEndpointUrl("audio1"), Times.Once);
        _mediaApiClientMock.Verify(x => x.GetMediaResourceEndpointUrl("sfx1"), Times.Once);
        _mediaApiClientMock.Verify(x => x.GetMediaResourceEndpointUrl("sfx2"), Times.Once);
        _mediaApiClientMock.Verify(x => x.GetMediaResourceEndpointUrl("music1"), Times.Once);
    }

    [Fact]
    public async Task CacheAudioAsync_ShouldHandleNetworkFailureGracefully()
    {
        // Arrange
        var mediaId = "fail1";
        _mediaApiClientMock.Setup(x => x.GetMediaResourceEndpointUrl(mediaId))
            .Returns("https://api.example.com/media/fail1");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network failure"));

        // Act
        await _sut.CacheAudioAsync(mediaId);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );

        // Should have logged a debug message for the exception
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pre-cache request for fail1 had an issue")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
