using FluentAssertions;
using Moq;
using Mystira.App.Domain.Models;
using Mystira.App.PWA.Services.Music;
using Xunit;

namespace Mystira.App.PWA.Tests.Services.Music;

public class SceneAudioOrchestratorTests
{
    private readonly Mock<IMusicResolver> _resolverMock;
    private readonly Mock<IAudioBus> _audioBusMock;
    private readonly SceneAudioOrchestrator _sut;

    public SceneAudioOrchestratorTests()
    {
        _resolverMock = new Mock<IMusicResolver>();
        _audioBusMock = new Mock<IAudioBus>();
        _sut = new SceneAudioOrchestrator(_resolverMock.Object, _audioBusMock.Object);
    }

    [Fact]
    public async Task EnterSceneAsync_ShouldPlayMusic_WhenResolverReturnsTrack()
    {
        // Arrange
        var scene = new Scene();
        var scenario = new Scenario();
        var resolution = new MusicResolutionResult 
        { 
            TrackId = "new_track", 
            Profile = MusicProfile.Cozy, 
            Transition = MusicTransitionHint.CrossfadeNormal 
        };

        _resolverMock.Setup(x => x.ResolveMusic(It.IsAny<Scene>(), It.IsAny<Scenario>(), It.IsAny<MusicContext>()))
            .Returns(resolution);

        // Act
        await _sut.EnterSceneAsync(scene, scenario);

        // Assert
        _audioBusMock.Verify(x => x.PlayMusicAsync("new_track", MusicTransitionHint.CrossfadeNormal, 1.0f), Times.Once);
    }

    [Fact]
    public async Task EnterSceneAsync_ShouldStopMusic_WhenResolverReturnsSilence()
    {
        // Arrange
        var scene = new Scene();
        var scenario = new Scenario();
        var resolutionSilence = new MusicResolutionResult 
        { 
            IsSilence = true,
            Transition = MusicTransitionHint.CrossfadeLong
        };
        
        var trackResolution = new MusicResolutionResult { TrackId = "track1", Profile = MusicProfile.Neutral };
        
        _resolverMock.SetupSequence(x => x.ResolveMusic(It.IsAny<Scene>(), It.IsAny<Scenario>(), It.IsAny<MusicContext>()))
            .Returns(trackResolution)
            .Returns(resolutionSilence);

        // Act 1 (Set state)
        await _sut.EnterSceneAsync(scene, scenario);
        
        // Act 2 (Silence)
        await _sut.EnterSceneAsync(scene, scenario);

        // Assert
        _audioBusMock.Verify(x => x.StopMusicAsync(MusicTransitionHint.CrossfadeLong), Times.Once);
    }

    [Fact]
    public async Task OnSceneActionAsync_ShouldPauseAll_WhenActionIsActive()
    {
        // Act
        await _sut.OnSceneActionAsync(true);

        // Assert
        _audioBusMock.Verify(x => x.PauseAllAsync(), Times.Once);
    }

    [Fact]
    public async Task OnSceneActionAsync_ShouldResumeAll_WhenActionIsInactive()
    {
        // Act
        await _sut.OnSceneActionAsync(false);

        // Assert
        _audioBusMock.Verify(x => x.ResumeAllAsync(), Times.Once);
    }
}
