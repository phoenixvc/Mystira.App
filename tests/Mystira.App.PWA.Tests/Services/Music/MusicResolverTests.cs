using FluentAssertions;
using Mystira.App.Domain.Models;
using Mystira.App.PWA.Services.Music;
using Xunit;

namespace Mystira.App.PWA.Tests.Services.Music;

public class MusicResolverTests
{
    private readonly MusicResolver _sut;

    public MusicResolverTests()
    {
        _sut = new MusicResolver();
    }

    [Fact]
    public void GetEffectiveIntent_ShouldReturnDefault_WhenSceneMusicIsNull()
    {
        // Arrange
        var scene = new Scene { Type = SceneType.Narrative, Music = null };

        // Act
        var result = _sut.GetEffectiveIntent(scene);

        // Assert
        result.Should().NotBeNull();
        result.Profile.Should().Be(MusicProfile.Neutral); // Default for Narrative
        result.Continuity.Should().Be(MusicContinuity.PreferContinue);
    }

    [Fact]
    public void ResolveMusic_ShouldForceChange_WhenIntentIsForceChange()
    {
        // Arrange
        var scene = new Scene 
        { 
            Type = SceneType.Narrative,
            Music = new SceneMusicSettings { Continuity = MusicContinuity.ForceChange, Profile = MusicProfile.Tense }
        };
        var scenario = new Scenario 
        { 
            MusicPalette = new MusicPalette 
            { 
                TracksByProfile = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) 
                { 
                    { "Tense", new List<string> { "track1" } } 
                } 
            } 
        };
        var context = new MusicContext { CurrentTrackId = "old_track", CurrentProfile = MusicProfile.Neutral };

        // Act
        var result = _sut.ResolveMusic(scene, scenario, context);

        // Assert
        result.IsSilence.Should().BeFalse();
        result.TrackId.Should().Be("track1");
    }

    [Fact]
    public void ResolveMusic_ShouldSilence_WhenIntentIsSilence()
    {
        // Arrange
        var scene = new Scene 
        { 
             Music = new SceneMusicSettings { Profile = MusicProfile.None }
        };
        var scenario = new Scenario();
        var context = new MusicContext { CurrentTrackId = "old_track" };

        // Act
        var result = _sut.ResolveMusic(scene, scenario, context);

        // Assert
        result.IsSilence.Should().BeTrue();
    }
}
