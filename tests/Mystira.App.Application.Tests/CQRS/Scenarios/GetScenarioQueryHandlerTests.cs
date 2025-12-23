using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class GetScenarioQueryHandlerTests : CqrsIntegrationTestBase
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
                AgeGroup = "6-9"
            },
            new Scenario
            {
                Id = "scenario-2",
                Title = "Mountain Mystery",
                Description = "Explore the mountain",
                AgeGroup = "10-13"
            }
        };

        await DbContext.Scenarios.AddRangeAsync(scenarios);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsScenario()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenarioQuery("scenario-1"));

        result.Should().NotBeNull();
        result!.Title.Should().Be("The Forest Quest");
        result.Description.Should().Be("Adventure in the forest");
        result.AgeGroup.Should().Be("6-9");
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsNull()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenarioQuery("invalid-id"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsNull()
    {
        var result = await Mediator.Send(new GetScenarioQuery("scenario-1"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNullScenarioId_ThrowsArgumentException()
    {
        var act = async () => await Mediator.Send(new GetScenarioQuery(null!));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Handle_WithEmptyScenarioId_ThrowsArgumentException(string scenarioId)
    {
        var act = async () => await Mediator.Send(new GetScenarioQuery(scenarioId));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_RetrievesCorrectScenarioAmongMany()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetScenarioQuery("scenario-2"));

        result.Should().NotBeNull();
        result!.Title.Should().Be("Mountain Mystery");
        result.Id.Should().Be("scenario-2");
    }
}
