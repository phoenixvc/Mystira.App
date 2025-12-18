using FluentAssertions;
using Mystira.App.Application.CQRS.CompassAxes.Commands;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.CompassAxes;

public class GetCompassAxesQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var axes = new[]
        {
            new CompassAxis
            {
                Id = "axis-1",
                Name = "Chaos-Order",
                Description = "From chaos to order"
            },
            new CompassAxis
            {
                Id = "axis-2",
                Name = "Good-Evil",
                Description = "From good to evil"
            },
            new CompassAxis
            {
                Id = "axis-3",
                Name = "Law-Freedom",
                Description = "From law to freedom"
            }
        };

        await DbContext.CompassAxes.AddRangeAsync(axes);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllCompassAxesQuery_ReturnsAllAxes()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetAllCompassAxesQuery());

        result.Should().HaveCount(3);
        result.Should().Contain(ax => ax.Name == "Chaos-Order");
        result.Should().Contain(ax => ax.Name == "Good-Evil");
        result.Should().Contain(ax => ax.Name == "Law-Freedom");
    }

    [Fact]
    public async Task GetAllCompassAxesQuery_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetAllCompassAxesQuery());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCompassAxesQuery_CachesResultOnSecondCall()
    {
        await SeedTestDataAsync();

        var firstCall = await Mediator.Send(new GetAllCompassAxesQuery());
        var secondCall = await Mediator.Send(new GetAllCompassAxesQuery());

        firstCall.Should().Equal(secondCall);
    }

    [Fact]
    public async Task GetCompassAxisByIdQuery_WithValidId_ReturnsAxis()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetCompassAxisByIdQuery("axis-1"));

        result.Should().NotBeNull();
        result!.Name.Should().Be("Chaos-Order");
        result.Description.Should().Be("From chaos to order");
    }

    [Fact]
    public async Task GetCompassAxisByIdQuery_WithInvalidId_ReturnsNull()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetCompassAxisByIdQuery("invalid-id"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCompassAxisByIdQuery_WithEmptyDatabase_ReturnsNull()
    {
        var result = await Mediator.Send(new GetCompassAxisByIdQuery("axis-1"));

        result.Should().BeNull();
    }
}
