using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Commands;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class DeleteScenarioCommandHandlerTests : CqrsIntegrationTestBase
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
    public async Task Handle_WithValidId_DeletesScenario()
    {
        await SeedTestDataAsync();

        var act = async () => await Mediator.Send(new DeleteScenarioCommand("scenario-1"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_RemovesEntityFromDatabase()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteScenarioCommand("scenario-1"));

        var deleted = await DbContext.Scenarios.FindAsync("scenario-1");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeletesOnlyTargetEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteScenarioCommand("scenario-1"));

        var remaining = await Mediator.Send(new GetScenariosQuery());
        remaining.Should().HaveCount(1);
        remaining.First().Id.Should().Be("scenario-2");
    }

    [Fact]
    public async Task Handle_WithInvalidId_ThrowsInvalidOperationException()
    {
        var act = async () => await Mediator.Send(new DeleteScenarioCommand("invalid-id"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Scenario not found: invalid-id");
    }

    [Fact]
    public async Task Handle_WithNullScenarioId_ThrowsArgumentException()
    {
        var act = async () => await Mediator.Send(new DeleteScenarioCommand(null!));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Handle_WithEmptyScenarioId_ThrowsArgumentException(string scenarioId)
    {
        var act = async () => await Mediator.Send(new DeleteScenarioCommand(scenarioId));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedId_ThrowsInvalidOperationException()
    {
        await SeedTestDataAsync();
        await Mediator.Send(new DeleteScenarioCommand("scenario-1"));

        var act = async () => await Mediator.Send(new DeleteScenarioCommand("scenario-1"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
