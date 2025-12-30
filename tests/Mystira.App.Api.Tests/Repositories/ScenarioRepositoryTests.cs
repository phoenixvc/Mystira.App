using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mystira.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;
using Xunit;

namespace Mystira.App.Api.Tests.Repositories;

public class ScenarioRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnScenarioWithAllFields()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MystiraAppDbContext>()
            .UseInMemoryDatabase(databaseName: "ScenarioTestDatabase_" + Guid.NewGuid())
            .Options;

        using (var context = new MystiraAppDbContext(options))
        {
            // We'll use a raw SQL approach or just trust that if we set it in the model, EF handles it.
            // Since it's InMemoryDatabase, we can't easily test the JSON string conversion from DB.
            // But we can test if EF handles the model we give it.
            var scenario = new Scenario
            {
                Id = "test-scenario-1",
                Title = "Test Scenario",
                Description = "A test scenario description",
                Image = "test-image-id",
                MusicPalette = new MusicPalette
                {
                    DefaultProfile = MusicProfile.Cozy, // This will be saved as "Cozy" by my new converter
                    TracksByProfile = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Cozy", new List<string> { "track1.mp3" } }
                    }
                },
                Scenes = new List<Scene>
                {
                    new Scene
                    {
                        Id = "start",
                        Title = "Start Scene",
                        Description = "The beginning",
                        Music = new SceneMusicSettings
                        {
                            Profile = MusicProfile.Cozy,
                            Energy = 0.5
                        },
                        SoundEffects = new List<SceneSoundEffect>
                        {
                            new SceneSoundEffect { Track = "sfx1.wav", Loopable = true }
                        }
                    }
                }
            };

            context.Scenarios.Add(scenario);
            await context.SaveChangesAsync();
        }

        using (var context = new MystiraAppDbContext(options))
        {
            var repository = new ScenarioRepository(context);

            // Act
            var result = await repository.GetByIdAsync("test-scenario-1");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be("test-scenario-1");
            result.Title.Should().Be("Test Scenario");
            result.Image.Should().Be("test-image-id");

            // MusicPalette should not be null
            result.MusicPalette.Should().NotBeNull();
            result.MusicPalette!.DefaultProfile.Should().Be(MusicProfile.Cozy);
            result.MusicPalette.TracksByProfile.Should().ContainKey("Cozy");
            result.MusicPalette.TracksByProfile["Cozy"].Should().Contain("track1.mp3");

            // Scenes should have music and sfx
            result.Scenes.Should().HaveCount(1);
            var scene = result.Scenes[0];
            scene.Music.Should().NotBeNull();
            scene.Music!.Profile.Should().Be(MusicProfile.Cozy);
            scene.Music.Energy.Should().Be(0.5);

            scene.SoundEffects.Should().HaveCount(1);
            scene.SoundEffects[0].Track.Should().Be("sfx1.wav");
            scene.SoundEffects[0].Loopable.Should().BeTrue();
        }
    }
}
