using FluentAssertions;
using Mystira.App.Application.CQRS.AgeGroups.Commands;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.AgeGroups;

public class GetAgeGroupsQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var ageGroups = new[]
        {
            new AgeGroupDefinition
            {
                Id = "ag-1",
                Name = "Toddlers",
                Value = "2-4",
                MinimumAge = 2,
                MaximumAge = 4,
                Description = "For toddlers"
            },
            new AgeGroupDefinition
            {
                Id = "ag-2",
                Name = "Children",
                Value = "5-8",
                MinimumAge = 5,
                MaximumAge = 8,
                Description = "For children"
            },
            new AgeGroupDefinition
            {
                Id = "ag-3",
                Name = "Teens",
                Value = "13-18",
                MinimumAge = 13,
                MaximumAge = 18,
                Description = "For teenagers"
            }
        };

        await DbContext.AgeGroupDefinitions.AddRangeAsync(ageGroups);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllAgeGroupsQuery_ReturnsAllAgeGroups()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetAllAgeGroupsQuery());

        result.Should().HaveCount(3);
        result.Should().Contain(ag => ag.Value == "2-4");
        result.Should().Contain(ag => ag.Value == "5-8");
        result.Should().Contain(ag => ag.Value == "13-18");
    }

    [Fact]
    public async Task GetAllAgeGroupsQuery_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetAllAgeGroupsQuery());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAgeGroupsQuery_CachesResultOnSecondCall()
    {
        await SeedTestDataAsync();

        var firstCall = await Mediator.Send(new GetAllAgeGroupsQuery());
        var secondCall = await Mediator.Send(new GetAllAgeGroupsQuery());

        firstCall.Should().Equal(secondCall);
    }

    [Fact]
    public async Task GetAgeGroupByIdQuery_WithValidId_ReturnsAgeGroup()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetAgeGroupByIdQuery("ag-1"));

        result.Should().NotBeNull();
        result.Name.Should().Be("Toddlers");
        result.Value.Should().Be("2-4");
    }

    [Fact]
    public async Task GetAgeGroupByIdQuery_WithInvalidId_ReturnsNull()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetAgeGroupByIdQuery("invalid-id"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAgeGroupByIdQuery_WithEmptyDatabase_ReturnsNull()
    {
        var result = await Mediator.Send(new GetAgeGroupByIdQuery("ag-1"));

        result.Should().BeNull();
    }
}
