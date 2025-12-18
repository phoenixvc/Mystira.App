using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class GetFeaturedScenariosQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var scenarios = new[]
        {
            new Scenario
            {
                Id = "scenario-1",
                Title = "Featured Quest",
                Description = "Featured scenario",
                AgeGroup = "6-9",
                IsFeatured = true,
                IsActive = true
            },
            new Scenario
            {
                Id = "scenario-2",
                Title = "Another Featured",
                Description = "Another featured scenario",
                AgeGroup = "6-9",
                IsFeatured = true,
                IsActive = true
            },
            new Scenario
            {
                Id = "scenario-3",
                Title = "Non-Featured Quest",
                Description = "Regular scenario",
                AgeGroup = "6-9",
                IsFeatured = false,
                IsActive = true
            },
            new Scenario
            {
                Id = "scenario-4",
                Title = "Inactive Featured",
                Description = "Inactive scenario",
                AgeGroup = "6-9",
                IsFeatured = true,
                IsActive = false
            }
        };

        await DbContext.Scenarios.AddRangeAsync(scenarios);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_ReturnsFeaturedAndActiveScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetFeaturedScenariosQuery());

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.IsFeatured.Should().BeTrue());
        result.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ExcludesNonFeaturedScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetFeaturedScenariosQuery());

        result.Should().NotContain(s => s.Id == "scenario-3");
    }

    [Fact]
    public async Task Handle_ExcludesInactiveScenarios()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetFeaturedScenariosQuery());

        result.Should().NotContain(s => s.Id == "scenario-4");
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetFeaturedScenariosQuery());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNoFeaturedScenarios_ReturnsEmptyList()
    {
        var scenarios = new[]
        {
            new Scenario
            {
                Id = "scenario-1",
                Title = "Regular Scenario",
                Description = "Not featured",
                AgeGroup = "6-9",
                IsFeatured = false,
                IsActive = true
            }
        };

        await DbContext.Scenarios.AddRangeAsync(scenarios);
        await DbContext.SaveChangesAsync();

        var result = await Mediator.Send(new GetFeaturedScenariosQuery());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsScenariosOrderedByTitle()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetFeaturedScenariosQuery());

        var titles = result.Select(s => s.Title).ToList();
        titles.Should().Equal(titles.OrderBy(t => t));
    }
}
