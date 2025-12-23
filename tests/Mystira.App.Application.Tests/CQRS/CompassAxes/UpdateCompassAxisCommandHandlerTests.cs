using FluentAssertions;
using Mystira.App.Application.CQRS.CompassAxes.Commands;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.CompassAxes;

public class UpdateCompassAxisCommandHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var axis = new CompassAxis
        {
            Id = "axis-1",
            Name = "Chaos-Order",
            Description = "From chaos to order",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await DbContext.CompassAxes.AddAsync(axis);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesCompassAxis()
    {
        await SeedTestDataAsync();

        var command = new UpdateCompassAxisCommand(
            Id: "axis-1",
            Name: "Organized-Chaotic",
            Description: "Updated description"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result!.Id.Should().Be("axis-1");
        result.Name.Should().Be("Organized-Chaotic");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNull()
    {
        var command = new UpdateCompassAxisCommand(
            Id: "invalid-id",
            Name: "Good-Evil",
            Description: "For good-evil axis"
        );

        var result = await Mediator.Send(command);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UpdatesPersistedEntity()
    {
        await SeedTestDataAsync();

        var command = new UpdateCompassAxisCommand(
            Id: "axis-1",
            Name: "New-Name",
            Description: "New description"
        );

        await Mediator.Send(command);

        var updated = await DbContext.CompassAxes.FindAsync("axis-1");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New-Name");
        updated.Description.Should().Be("New description");
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp()
    {
        await SeedTestDataAsync();
        var originalAxis = await DbContext.CompassAxes.FindAsync("axis-1");
        var originalUpdateTime = originalAxis!.UpdatedAt;

        await Task.Delay(100);

        var command = new UpdateCompassAxisCommand(
            Id: "axis-1",
            Name: "Updated",
            Description: "Updated description"
        );

        var result = await Mediator.Send(command);

        result!.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        await SeedTestDataAsync();
        var initialAxes = await Mediator.Send(new GetAllCompassAxesQuery());

        var command = new UpdateCompassAxisCommand(
            Id: "axis-1",
            Name: "Modified",
            Description: "Modified description"
        );

        await Mediator.Send(command);
        var updatedAxes = await Mediator.Send(new GetAllCompassAxesQuery());

        updatedAxes.Should().HaveCount(initialAxes.Count);
        updatedAxes.Should().Contain(ax => ax.Name == "Modified");
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_UpdatesWithEmpty()
    {
        await SeedTestDataAsync();

        var command = new UpdateCompassAxisCommand(
            Id: "axis-1",
            Name: "Updated",
            Description: string.Empty
        );

        var result = await Mediator.Send(command);

        result!.Description.Should().Be(string.Empty);
    }
}
