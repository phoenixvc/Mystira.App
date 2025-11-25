// Example CQRS Query Test
// File: tests/Mystira.App.Application.Tests/CQRS/Scenarios/GetScenarioQueryTests.cs

using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

/// <summary>
/// Example tests for a CQRS query handler
/// Demonstrates testing query execution, filtering, and caching behavior
/// </summary>
public class GetScenarioQueryTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var scenarios = new List<Scenario>
        {
            new()
            {
                Id = "scenario-1",
                Title = "The Dragon's Lair",
                Description = "Face the dragon",
                Genre = "Fantasy",
                AgeGroup = "8-10",
                IsActive = true
            },
            new()
            {
                Id = "scenario-2",
                Title = "Space Station Mystery",
                Description = "Solve the mystery",
                Genre = "SciFi",
                AgeGroup = "10-12",
                IsActive = true
            },
            new()
            {
                Id = "scenario-3",
                Title = "Inactive Scenario",
                Description = "This is inactive",
                Genre = "Fantasy",
                AgeGroup = "8-10",
                IsActive = false
            }
        };

        DbContext.Scenarios.AddRange(scenarios);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetScenarioQuery_WithValidId_ReturnsScenario()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("scenario-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("scenario-1");
        result.Title.Should().Be("The Dragon's Lair");
    }

    [Fact]
    public async Task GetScenarioQuery_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("non-existent-id");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllScenariosQuery_ReturnsOnlyActiveScenarios()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllScenariosQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().HaveCount(2, "only active scenarios should be returned");
        result.Should().OnlyContain(s => s.IsActive);
        result.Should().NotContain(s => s.Id == "scenario-3");
    }

    [Fact]
    public async Task GetScenariosByGenreQuery_FiltersCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenariosByGenreQuery("Fantasy");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().HaveCount(1, "only one active fantasy scenario exists");
        result.Should().OnlyContain(s => s.Genre == "Fantasy" && s.IsActive);
    }

    [Fact]
    public async Task GetScenarioQuery_CachesResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("scenario-1");

        // Act - First call (database hit)
        var result1 = await Mediator.Send(query);

        // Modify the database directly (bypassing command)
        var scenario = await DbContext.Scenarios.FindAsync("scenario-1");
        scenario!.Title = "Modified Title";
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act - Second call (should return cached result)
        var result2 = await Mediator.Send(query);

        // Assert
        result1!.Title.Should().Be("The Dragon's Lair");
        result2!.Title.Should().Be("The Dragon's Lair", "cache should return original value");
    }

    [Fact]
    public async Task GetScenarioQuery_CacheInvalidation_RefreshesData()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("scenario-1");

        // Act - First call (populates cache)
        var result1 = await Mediator.Send(query);

        // Invalidate cache explicitly
        CacheInvalidation.InvalidateQueriesForEntity("Scenario");
        ClearCache();

        // Modify database
        var scenario = await DbContext.Scenarios.FindAsync("scenario-1");
        scenario!.Title = "Updated Title";
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act - Second call (should hit database)
        var result2 = await Mediator.Send(query);

        // Assert
        result1!.Title.Should().Be("The Dragon's Lair");
        result2!.Title.Should().Be("Updated Title", "cache was invalidated");
    }

    [Fact]
    public async Task GetScenariosByAgeGroupQuery_FiltersCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenariosByAgeGroupQuery("8-10");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(s => s.AgeGroup == "8-10" && s.IsActive);
    }
}
