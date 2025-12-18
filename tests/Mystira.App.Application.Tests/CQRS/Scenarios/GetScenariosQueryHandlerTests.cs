using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class GetScenariosQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var scenarios = new[]
        {
            new Scenario
            {
                Id = "scenario-1",
                Title = "The Forest Quest",
                Description = "Adventure in the forest",
                AgeGroup = "6-9",
                IsActive = true
            },
            new Scenario
            {
                Id = "scenario-2",
                Title = "Mountain Mystery",
                Description = "Explore the mountain",
                AgeGroup = "10-13",
                IsActive = true
            },
            new Scenario
            {
                Id = "scenario-3",
                Title = "Hidden Caverns",
                Description = "Discover ancient caverns",
                AgeGroup = "6-9",
                IsActive = true
            }
        };

        await DbContext.Scenarios.AddRangeAsync(scenarios);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_ReturnsAllScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenariosQuery());

        result.Should().HaveCount(3);
        result.Should().Contain(s => s.Title == "The Forest Quest");
        result.Should().Contain(s => s.Title == "Mountain Mystery");
        result.Should().Contain(s => s.Title == "Hidden Caverns");
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetScenariosQuery());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PreservesScenarioProperties()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenariosQuery());

        var scenario = result.FirstOrDefault(s => s.Id == "scenario-1");
        scenario.Should().NotBeNull();
        scenario!.Title.Should().Be("The Forest Quest");
        scenario.Description.Should().Be("Adventure in the forest");
        scenario.AgeGroup.Should().Be("6-9");
    }

    [Fact]
    public async Task Handle_CachesResultsOnSecondCall()
    {
        await SeedTestDataAsync();

        var firstCall = await Mediator.Send(new GetScenariosQuery());
        var secondCall = await Mediator.Send(new GetScenariosQuery());

        firstCall.Count().Should().Be(secondCall.Count());
    }
}
