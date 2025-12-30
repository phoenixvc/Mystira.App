using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mystira.Domain.Models;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class ScenarioApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ScenarioApiClient>> _loggerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly ScenarioApiClient _client;
    private readonly string _baseUrl = "http://localhost:5000/";

    public ScenarioApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_baseUrl)
        };

        _loggerMock = new Mock<ILogger<ScenarioApiClient>>();
        _tokenProviderMock = new Mock<ITokenProvider>();

        _client = new ScenarioApiClient(_httpClient, _loggerMock.Object, _tokenProviderMock.Object);
    }

    private void SetupResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task GetScenarioAsync_ShouldDeserializeMusicPaletteAndSceneMusic()
    {
        // Arrange
        var scenarioJson = @"
{
    ""id"": ""scen-1"",
    ""title"": ""Test Scenario"",
    ""musicPalette"": {
        ""defaultProfile"": ""Cozy"",
        ""tracksByProfile"": {
            ""Cozy"": [""track1.mp3""],
            ""Action"": [""track2.mp3""]
        }
    },
    ""scenes"": [
        {
            ""id"": ""scene-1"",
            ""title"": ""Scene 1"",
            ""music"": {
                ""profile"": ""Cozy"",
                ""energy"": 0.5,
                ""continuity"": ""PreferContinue""
            },
            ""soundEffects"": [
                { ""track"": ""sfx1.wav"", ""loopable"": true, ""energy"": 0.8 }
            ]
        }
    ]
}";
        SetupResponse(HttpStatusCode.OK, scenarioJson);

        // Act
        var result = await _client.GetScenarioAsync("scen-1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("scen-1");

        // MusicPalette
        result.MusicPalette.Should().NotBeNull();
        result.MusicPalette!.DefaultProfile.Should().Be(MusicProfile.Cozy);
        result.MusicPalette.TracksByProfile.Should().ContainKey("Cozy");
        result.MusicPalette.TracksByProfile["Cozy"].Should().Contain("track1.mp3");

        // Scene Music
        result.Scenes.Should().HaveCount(1);
        var scene = result.Scenes[0];
        scene.Music.Should().NotBeNull();
        scene.Music!.Profile.Should().Be(MusicProfile.Cozy);
        scene.Music.Energy.Should().Be(0.5);

        // Scene Sound Effects
        scene.SoundEffects.Should().HaveCount(1);
        scene.SoundEffects[0].Track.Should().Be("sfx1.wav");
        scene.SoundEffects[0].Loopable.Should().BeTrue();
    }

    [Fact]
    public async Task GetScenarioAsync_ShouldHandlePascalCaseJsonFromApi()
    {
        // Arrange
        var scenarioJson = @"
{
    ""id"": ""scen-pascal"",
    ""Title"": ""Pascal Scenario"",
    ""MusicPalette"": {
        ""DefaultProfile"": ""Cozy"",
        ""TracksByProfile"": {
            ""Cozy"": [""track1.mp3""]
        }
    },
    ""Scenes"": [
        {
            ""Id"": ""scene-1"",
            ""Title"": ""Scene 1"",
            ""Music"": {
                ""Profile"": ""Cozy"",
                ""Energy"": 0.5
            }
        }
    ]
}";
        SetupResponse(HttpStatusCode.OK, scenarioJson);

        // Act
        var result = await _client.GetScenarioAsync("scen-pascal");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("scen-pascal");
        result.Title.Should().Be("Pascal Scenario");
        result.MusicPalette.Should().NotBeNull();
        result.MusicPalette!.DefaultProfile.Should().Be(MusicProfile.Cozy);
        result.Scenes.Should().HaveCount(1);
        result.Scenes[0].Title.Should().Be("Scene 1");
        result.Scenes[0].Music.Should().NotBeNull();
    }
}
