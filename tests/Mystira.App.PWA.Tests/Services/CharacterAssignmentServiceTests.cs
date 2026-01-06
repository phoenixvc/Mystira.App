using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Domain.Models;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Services;
using Mystira.App.PWA.Services.Music;
using Xunit;
using Scene = Mystira.App.PWA.Models.Scene;
using Scenario = Mystira.App.PWA.Models.Scenario;
using GameSession = Mystira.App.PWA.Models.GameSession;

namespace Mystira.App.PWA.Tests.Services;

public class CharacterAssignmentServiceTests
{
    private readonly Mock<ILogger<CharacterAssignmentService>> _loggerMock;
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IGameSessionService> _gameSessionServiceMock;
    private readonly Mock<IMusicResolver> _musicResolverMock;
    private readonly Mock<IAudioBus> _audioBusMock;
    private readonly IAudioStateStore _stateStore;
    private readonly SceneAudioOrchestrator _audioOrchestrator;
    private readonly CharacterAssignmentService _sut;

    public CharacterAssignmentServiceTests()
    {
        _loggerMock = new Mock<ILogger<CharacterAssignmentService>>();
        _apiClientMock = new Mock<IApiClient>();
        _authServiceMock = new Mock<IAuthService>();
        _gameSessionServiceMock = new Mock<IGameSessionService>();
        _musicResolverMock = new Mock<IMusicResolver>();
        _audioBusMock = new Mock<IAudioBus>();
        _stateStore = new AudioStateStore();
        _audioOrchestrator = new SceneAudioOrchestrator(_musicResolverMock.Object, _audioBusMock.Object, _stateStore);

        _sut = new CharacterAssignmentService(
            _loggerMock.Object,
            _apiClientMock.Object,
            _authServiceMock.Object,
            _gameSessionServiceMock.Object,
            _audioOrchestrator);
    }

    [Fact]
    public async Task StartGameSessionWithAssignmentsAsync_ShouldOrchestrateAudioForStartingScene()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "scenario-1",
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Starting Scene" }
            }
        };

        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            Scenario = scenario,
            CharacterAssignments = new List<CharacterAssignment>()
        };

        var apiSession = new GameSession
        {
            Id = "session-1",
            StartedAt = DateTime.UtcNow
        };

        _apiClientMock.Setup(x => x.StartGameSessionWithAssignmentsAsync(request))
            .ReturnsAsync(apiSession);

        _musicResolverMock.Setup(x => x.ResolveMusic(It.IsAny<Scene>(), It.IsAny<Scenario>(), It.IsAny<MusicContext>()))
            .Returns(new MusicResolutionResult { TrackId = "track-1", Transition = MusicTransitionHint.CrossfadeNormal });

        _musicResolverMock.Setup(x => x.GetEffectiveIntent(It.IsAny<Scene>()))
            .Returns(new SceneMusicSettings { Energy = 0.5 });

        // Act
        var result = await _sut.StartGameSessionWithAssignmentsAsync(request);

        // Assert
        result.Should().BeTrue();
        _audioBusMock.Verify(x => x.PlayMusicAsync("track-1", MusicTransitionHint.CrossfadeNormal, 0.5f), Times.Once);
    }
}
