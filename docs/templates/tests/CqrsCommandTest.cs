// Example CQRS Command Test
// File: tests/Mystira.App.Application.Tests/CQRS/Scenarios/CreateScenarioCommandTests.cs

using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Commands;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

/// <summary>
/// Example tests for a CQRS command handler
/// Demonstrates testing command execution, validation, and persistence
/// Uses CqrsIntegrationTestBase for in-memory database and MediatR
/// </summary>
public class CreateScenarioCommandTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed content bundles (required for scenario creation)
        var bundles = new List<ContentBundle>
        {
            new()
            {
                Id = "bundle-1",
                Title = "Fantasy Adventures",
                Description = "Collection of fantasy scenarios",
                IsActive = true
            }
        };

        DbContext.ContentBundles.AddRange(bundles);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateScenarioCommand_WithValidRequest_CreatesScenario()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateScenarioRequest
        {
            Title = "The Dragon's Lair",
            Description = "Face the dragon and claim the treasure",
            Genre = "Fantasy",
            AgeGroup = "8-10",
            BundleId = "bundle-1",
            Scenes = new List<CreateSceneRequest>
            {
                new()
                {
                    OrderIndex = 1,
                    Narrative = "You approach the cave entrance...",
                    Choices = new List<CreateChoiceRequest>
                    {
                        new() { Text = "Enter bravely", CompassEffect = new() { Courage = 10 } },
                        new() { Text = "Scout the area first", CompassEffect = new() { Wisdom = 10 } }
                    }
                }
            }
        };
        var command = new CreateScenarioCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Title.Should().Be("The Dragon's Lair");
        result.Genre.Should().Be("Fantasy");
        result.Scenes.Should().HaveCount(1);
        result.Scenes[0].Choices.Should().HaveCount(2);

        // Verify persistence
        DbContext.ChangeTracker.Clear();
        var savedScenario = await DbContext.Scenarios.FindAsync(result.Id);
        savedScenario.Should().NotBeNull();
        savedScenario!.Title.Should().Be("The Dragon's Lair");
    }

    [Fact]
    public async Task CreateScenarioCommand_WithMissingTitle_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateScenarioRequest
        {
            Title = "", // Invalid: empty title
            Description = "Test description",
            Genre = "Fantasy",
            AgeGroup = "8-10",
            BundleId = "bundle-1"
        };
        var command = new CreateScenarioCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task CreateScenarioCommand_WithInvalidBundleId_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateScenarioRequest
        {
            Title = "Test Scenario",
            Description = "Test description",
            Genre = "Fantasy",
            AgeGroup = "8-10",
            BundleId = "non-existent-bundle" // Invalid: bundle doesn't exist
        };
        var command = new CreateScenarioCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task CreateScenarioCommand_SetsCreatedTimestamp()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateScenarioRequest
        {
            Title = "Test Scenario",
            Description = "Test description",
            Genre = "Fantasy",
            AgeGroup = "8-10",
            BundleId = "bundle-1"
        };
        var command = new CreateScenarioCommand(request);
        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.CreatedAt.Should().BeAfter(beforeCreation);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateScenarioCommand_WithMultipleScenes_CreatesInCorrectOrder()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateScenarioRequest
        {
            Title = "Multi-scene Adventure",
            Description = "An adventure with multiple scenes",
            Genre = "Fantasy",
            AgeGroup = "8-10",
            BundleId = "bundle-1",
            Scenes = new List<CreateSceneRequest>
            {
                new() { OrderIndex = 1, Narrative = "Scene 1" },
                new() { OrderIndex = 2, Narrative = "Scene 2" },
                new() { OrderIndex = 3, Narrative = "Scene 3" }
            }
        };
        var command = new CreateScenarioCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Scenes.Should().HaveCount(3);
        result.Scenes[0].OrderIndex.Should().Be(1);
        result.Scenes[1].OrderIndex.Should().Be(2);
        result.Scenes[2].OrderIndex.Should().Be(3);
    }

    [Fact]
    public async Task CreateScenarioCommand_CausesQueryCacheInvalidation()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateScenarioRequest
        {
            Title = "Cache Test Scenario",
            Description = "Test cache invalidation",
            Genre = "Fantasy",
            AgeGroup = "8-10",
            BundleId = "bundle-1"
        };
        var command = new CreateScenarioCommand(request);

        // Pre-populate cache with scenario list query
        var listQuery = new GetAllScenariosQuery();
        var cachedResult = await Mediator.Send(listQuery);
        var initialCount = cachedResult.Count();

        // Act
        var newScenario = await Mediator.Send(command);

        // Query again - should reflect new scenario (cache invalidated)
        var updatedResult = await Mediator.Send(listQuery);

        // Assert
        updatedResult.Should().HaveCountGreaterThan(initialCount);
        updatedResult.Should().Contain(s => s.Id == newScenario.Id);
    }
}
