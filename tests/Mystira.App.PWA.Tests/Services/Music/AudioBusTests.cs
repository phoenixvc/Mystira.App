using Microsoft.JSInterop;
using Moq;
using Mystira.Domain.Models;
using Mystira.App.PWA.Services;
using Mystira.App.PWA.Services.Music;
using Xunit;

namespace Mystira.App.PWA.Tests.Services.Music;

public class AudioBusTests
{
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly Mock<IApiEndpointCache> _endpointCacheMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly AudioBus _sut;

    public AudioBusTests()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _endpointCacheMock = new Mock<IApiEndpointCache>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _sut = new AudioBus(_jsRuntimeMock.Object, _endpointCacheMock.Object, _settingsServiceMock.Object);

        // Default to audio enabled
        _settingsServiceMock.Setup(x => x.GetAudioEnabledAsync()).ReturnsAsync(true);
        _endpointCacheMock.Setup(x => x.ApiBaseUrl).Returns("https://api.example.com/");
    }

    [Fact]
    public async Task PlayMusicAsync_ShouldUseEndpointCache()
    {
        // Arrange
        var trackId = "test-track";
        var transition = MusicTransitionHint.CrossfadeNormal;
        var volume = 0.5f;
        var expectedUrl = "https://api.example.com/api/media/test-track";

        // Setup import
        var moduleMock = new Mock<IJSObjectReference>();
        _jsRuntimeMock.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleMock.Object);

        // Act
        await _sut.PlayMusicAsync(trackId, transition, volume);

        // Assert
        moduleMock.Verify(x => x.InvokeAsync<object>(
            "playMusic",
            It.Is<object[]>(args =>
                (string)args[0] == expectedUrl &&
                (string)args[1] == transition.ToString() &&
                (float)args[2] == volume)),
            Times.Once);
    }

    [Fact]
    public async Task PlaySoundEffectAsync_ShouldUseEndpointCache()
    {
        // Arrange
        var trackId = "sfx-track";
        var loop = true;
        var volume = 0.8f;
        var expectedUrl = "https://api.example.com/api/media/sfx-track";

        // Setup import
        var moduleMock = new Mock<IJSObjectReference>();
        _jsRuntimeMock.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleMock.Object);

        // Act
        await _sut.PlaySoundEffectAsync(trackId, loop, volume);

        // Assert
        moduleMock.Verify(x => x.InvokeAsync<object>(
            "playSfx",
            It.Is<object[]>(args =>
                (string)args[0] == expectedUrl &&
                (bool)args[1] == loop &&
                (float)args[2] == volume)),
            Times.Once);
    }

    [Fact]
    public async Task StopSoundEffectAsync_ShouldUseEndpointCache()
    {
        // Arrange
        var trackId = "sfx-track";
        var expectedUrl = "https://api.example.com/api/media/sfx-track";

        // Setup import
        var moduleMock = new Mock<IJSObjectReference>();
        _jsRuntimeMock.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleMock.Object);

        // Act
        await _sut.StopSoundEffectAsync(trackId);

        // Assert
        moduleMock.Verify(x => x.InvokeAsync<object>(
            "stopSfx",
            It.Is<object[]>(args => (string)args[0] == expectedUrl)),
            Times.Once);
    }

    [Fact]
    public async Task PauseMusicAsync_ShouldCallJs()
    {
        // Setup import
        var moduleMock = new Mock<IJSObjectReference>();
        _jsRuntimeMock.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleMock.Object);

        // Act
        await _sut.PauseMusicAsync();

        // Assert
        moduleMock.Verify(x => x.InvokeAsync<object>(
            "pauseMusic",
            It.Is<object[]>(args => args.Length == 0)),
            Times.Once);
    }

    [Fact]
    public async Task ResumeMusicAsync_ShouldCallJs()
    {
        // Setup import
        var moduleMock = new Mock<IJSObjectReference>();
        _jsRuntimeMock.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleMock.Object);

        // Act
        await _sut.ResumeMusicAsync();

        // Assert
        moduleMock.Verify(x => x.InvokeAsync<object>(
            "resumeMusic",
            It.Is<object[]>(args => args.Length == 0)),
            Times.Once);
    }

    [Fact]
    public async Task IsMusicPausedAsync_ShouldCallJsAndReturnResult()
    {
        // Arrange
        var moduleMock = new Mock<IJSObjectReference>();
        _jsRuntimeMock.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(moduleMock.Object);

        moduleMock.Setup(x => x.InvokeAsync<bool>("isMusicPaused", It.IsAny<object[]>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsMusicPausedAsync();

        // Assert
        Assert.True(result);
        moduleMock.Verify(x => x.InvokeAsync<bool>(
            "isMusicPaused",
            It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task AllMethods_ShouldNotCallJs_WhenAudioDisabled()
    {
        // Arrange
        _settingsServiceMock.Setup(x => x.GetAudioEnabledAsync()).ReturnsAsync(false);

        // Act
        await _sut.PlayMusicAsync("track", MusicTransitionHint.HardCut);
        await _sut.StopMusicAsync(MusicTransitionHint.HardCut);
        await _sut.PlaySoundEffectAsync("sfx");
        await _sut.StopSoundEffectAsync("sfx");
        await _sut.SetMusicVolumeAsync(0.5f);
        await _sut.DuckMusicAsync(true);
        await _sut.PauseAllAsync();
        await _sut.ResumeAllAsync();
        await _sut.PauseMusicAsync();
        await _sut.ResumeMusicAsync();

        // Assert
        _jsRuntimeMock.Verify(x => x.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
    }
}
