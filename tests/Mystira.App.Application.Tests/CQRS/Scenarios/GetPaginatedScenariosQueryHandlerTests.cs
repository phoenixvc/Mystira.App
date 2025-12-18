using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class GetPaginatedScenariosQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var scenarios = Enumerable.Range(1, 15).Select(i => new Scenario
        {
            Id = $"scenario-{i}",
            Title = $"Scenario {i:D2}",
            Description = $"Description for scenario {i}",
            AgeGroup = "6-9"
        }).ToArray();

        await DbContext.Scenarios.AddRangeAsync(scenarios);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithFirstPage_ReturnsFirstPageOfScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetPaginatedScenariosQuery(
            PageNumber: 1,
            PageSize: 5
        ));

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithSecondPage_ReturnsSecondPageOfScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetPaginatedScenariosQuery(
            PageNumber: 2,
            PageSize: 5
        ));

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithLastPage_ReturnsRemainingScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetPaginatedScenariosQuery(
            PageNumber: 3,
            PageSize: 5
        ));

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithPageBeyondTotal_ReturnsEmptyList()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetPaginatedScenariosQuery(
            PageNumber: 10,
            PageSize: 5
        ));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithPageSizeOne_ReturnsSingleScenario()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetPaginatedScenariosQuery(
            PageNumber: 1,
            PageSize: 1
        ));

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetPaginatedScenariosQuery(
            PageNumber: 1,
            PageSize: 5
        ));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectScenarioCount()
    {
        await SeedTestDataAsync();

        var page1 = await Mediator.Send(new GetPaginatedScenariosQuery(1, 10));
        var page2 = await Mediator.Send(new GetPaginatedScenariosQuery(2, 10));

        page1.Count().Should().Be(10);
        page2.Count().Should().Be(5);
    }
}
