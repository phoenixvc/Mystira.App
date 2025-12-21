using FluentAssertions;
using Mystira.App.Application.CQRS.CompassAxes.Commands;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.CompassAxes;

public class DeleteCompassAxisCommandHandlerTests : CqrsIntegrationTestBase
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
            }
        };

        await DbContext.CompassAxes.AddRangeAsync(axes);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidId_DeletesCompassAxis()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new DeleteCompassAxisCommand("axis-1"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFalse()
    {
        var result = await Mediator.Send(new DeleteCompassAxisCommand("invalid-id"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SoftDeletesEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteCompassAxisCommand("axis-1"));

        var deleted = await DbContext.CompassAxes.FindAsync("axis-1");
        deleted.Should().NotBeNull();
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeletesOnlyTargetEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteCompassAxisCommand("axis-1"));

        var remaining = await Mediator.Send(new GetAllCompassAxesQuery());
        remaining.Should().HaveCount(1);
        remaining.First().Id.Should().Be("axis-2");
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        await SeedTestDataAsync();
        var initialAxes = await Mediator.Send(new GetAllCompassAxesQuery());
        initialAxes.Should().HaveCount(2, "Initial count should be 2");

        await Mediator.Send(new DeleteCompassAxisCommand("axis-1"));

        var updatedAxes = await Mediator.Send(new GetAllCompassAxesQuery());
        updatedAxes.Should().HaveCount(1, "Count should be 1 after deletion");
        updatedAxes.First().Id.Should().Be("axis-2");
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedId_ReturnsFalse()
    {
        await SeedTestDataAsync();
        await Mediator.Send(new DeleteCompassAxisCommand("axis-1"));

        var secondResult = await Mediator.Send(new DeleteCompassAxisCommand("axis-1"));

        secondResult.Should().BeFalse();
    }
}
