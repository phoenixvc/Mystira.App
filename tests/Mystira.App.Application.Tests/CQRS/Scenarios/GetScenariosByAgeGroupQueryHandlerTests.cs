using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class GetScenariosByAgeGroupQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var scenarios = new[]
        {
            new Scenario
            {
                Id = "scenario-1",
                Title = "Forest Quest",
                Description = "For young children",
                AgeGroup = "6-9"
            },
            new Scenario
            {
                Id = "scenario-2",
                Title = "Mountain Mystery",
                Description = "For young children",
                AgeGroup = "6-9"
            },
            new Scenario
            {
                Id = "scenario-3",
                Title = "Cavern Explorer",
                Description = "For teens",
                AgeGroup = "13-18"
            },
            new Scenario
            {
                Id = "scenario-4",
                Title = "Ancient Temple",
                Description = "For tweens",
                AgeGroup = "10-13"
            }
        };

        await DbContext.Scenarios.AddRangeAsync(scenarios);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidAgeGroup_ReturnsFilteredScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenariosByAgeGroupQuery("6-9"));

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.AgeGroup.Should().Be("6-9"));
    }

    [Fact]
    public async Task Handle_WithDifferentAgeGroup_ReturnsDifferentScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenariosByAgeGroupQuery("13-18"));

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Cavern Explorer");
    }

    [Fact]
    public async Task Handle_WithNonExistentAgeGroup_ReturnsEmptyList()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenariosByAgeGroupQuery("99-100"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetScenariosByAgeGroupQuery("6-9"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FiltersOnlyByAgeGroup()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenariosByAgeGroupQuery("10-13"));

        result.Should().HaveCount(1);
        result.First().Id.Should().Be("scenario-4");
        result.First().Title.Should().Be("Ancient Temple");
    }
}
