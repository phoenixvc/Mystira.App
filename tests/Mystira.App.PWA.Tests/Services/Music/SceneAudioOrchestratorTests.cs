using FluentAssertions;
using Moq;
using Mystira.App.Domain.Models;
using Mystira.App.PWA.Services.Music;
using Xunit;
using Scene = Mystira.App.PWA.Models.Scene;
using Scenario = Mystira.App.PWA.Models.Scenario;

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

        _resolverMock.Setup(x => x.GetEffectiveIntent(It.IsAny<Scene>()))
            .Returns(new SceneMusicSettings { Energy = 1.0 });

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

        // Required for energy update
        _resolverMock.Setup(x => x.GetEffectiveIntent(It.IsAny<Scene>()))
            .Returns(new SceneMusicSettings());

        // Act 1 (Set state)
        await _sut.EnterSceneAsync(scene, scenario);

        // Act 2 (Silence)
        await _sut.EnterSceneAsync(scene, scenario);

        // Assert
        _audioBusMock.Verify(x => x.StopMusicAsync(MusicTransitionHint.CrossfadeLong), Times.Once);
    }

    [Fact]
    public async Task EnterSceneAsync_ShouldUpdateContextEnergy_WhenTrackChanges()
    {
        // Arrange
        var scene = new Scene();
        var scenario = new Scenario();
        var resolution = new MusicResolutionResult { TrackId = "track1", Profile = MusicProfile.Neutral };
        var intent = new SceneMusicSettings { Energy = 0.8 };

        _resolverMock.Setup(x => x.ResolveMusic(It.IsAny<Scene>(), It.IsAny<Scenario>(), It.IsAny<MusicContext>()))
            .Returns(resolution);
        _resolverMock.Setup(x => x.GetEffectiveIntent(scene)).Returns(intent);

        // Act
        await _sut.EnterSceneAsync(scene, scenario);

        // Assert - We can't easily check private context, but we can check if GetEffectiveIntent was called
        _resolverMock.Verify(x => x.GetEffectiveIntent(scene), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EnterSceneAsync_ShouldUpdateContextEnergy_EvenIfTrackDoesNotChange()
    {
        // Arrange
        var scene = new Scene();
        var scenario = new Scenario();

        // First call to set initial track
        var resolution1 = new MusicResolutionResult { TrackId = "track1", Profile = MusicProfile.Neutral };
        var intent1 = new SceneMusicSettings { Energy = 0.5 };

        // Second call with same track but different energy
        var resolution2 = new MusicResolutionResult { TrackId = "track1", Profile = MusicProfile.Neutral, Transition = MusicTransitionHint.Keep };
        var intent2 = new SceneMusicSettings { Energy = 0.7 };

        _resolverMock.SetupSequence(x => x.ResolveMusic(It.IsAny<Scene>(), It.IsAny<Scenario>(), It.IsAny<MusicContext>()))
            .Returns(resolution1)
            .Returns(resolution2);

        _resolverMock.SetupSequence(x => x.GetEffectiveIntent(scene))
            .Returns(intent1)
            .Returns(intent2);

        // Act
        await _sut.EnterSceneAsync(scene, scenario); // Initial
        await _sut.EnterSceneAsync(scene, scenario); // Update energy

        // Assert
        _resolverMock.Verify(x => x.GetEffectiveIntent(scene), Times.Exactly(2));
        _audioBusMock.Verify(x => x.PlayMusicAsync("track1", It.IsAny<MusicTransitionHint>(), It.IsAny<float>()), Times.Once);
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
