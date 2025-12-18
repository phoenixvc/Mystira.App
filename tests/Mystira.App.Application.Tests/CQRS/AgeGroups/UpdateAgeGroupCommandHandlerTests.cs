using FluentAssertions;
using Mystira.App.Application.CQRS.AgeGroups.Commands;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.AgeGroups;

public class UpdateAgeGroupCommandHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var ageGroup = new AgeGroupDefinition
        {
            Id = "ag-1",
            Name = "Children",
            Value = "5-8",
            MinimumAge = 5,
            MaximumAge = 8,
            Description = "For children",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await DbContext.AgeGroupDefinitions.AddAsync(ageGroup);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesAgeGroup()
    {
        await SeedTestDataAsync();

        var command = new UpdateAgeGroupCommand(
            Id: "ag-1",
            Name: "Older Children",
            Value: "8-12",
            MinimumAge: 8,
            MaximumAge: 12,
            Description: "Updated description"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Id.Should().Be("ag-1");
        result.Name.Should().Be("Older Children");
        result.Value.Should().Be("8-12");
        result.MinimumAge.Should().Be(8);
        result.MaximumAge.Should().Be(12);
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNull()
    {
        var command = new UpdateAgeGroupCommand(
            Id: "invalid-id",
            Name: "Teens",
            Value: "13-18",
            MinimumAge: 13,
            MaximumAge: 18,
            Description: "For teenagers"
        );

        var result = await Mediator.Send(command);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UpdatesPersistedEntity()
    {
        await SeedTestDataAsync();

        var command = new UpdateAgeGroupCommand(
            Id: "ag-1",
            Name: "Toddlers",
            Value: "2-4",
            MinimumAge: 2,
            MaximumAge: 4,
            Description: "For toddlers"
        );

        await Mediator.Send(command);

        var updated = await DbContext.AgeGroupDefinitions.FindAsync("ag-1");
        updated.Should().NotBeNull();
        updated.Name.Should().Be("Toddlers");
        updated.Value.Should().Be("2-4");
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp()
    {
        await SeedTestDataAsync();
        var originalAgeGroup = await DbContext.AgeGroupDefinitions.FindAsync("ag-1");
        var originalUpdateTime = originalAgeGroup.UpdatedAt;

        await Task.Delay(100);

        var command = new UpdateAgeGroupCommand(
            Id: "ag-1",
            Name: "Updated",
            Value: "updated",
            MinimumAge: 0,
            MaximumAge: 100,
            Description: "Updated"
        );

        var result = await Mediator.Send(command);

        result.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        await SeedTestDataAsync();
        var initialGroups = await Mediator.Send(new GetAllAgeGroupsQuery());

        var command = new UpdateAgeGroupCommand(
            Id: "ag-1",
            Name: "Modified",
            Value: "modified",
            MinimumAge: 1,
            MaximumAge: 10,
            Description: "Modified"
        );

        await Mediator.Send(command);
        var updatedGroups = await Mediator.Send(new GetAllAgeGroupsQuery());

        updatedGroups.Should().HaveCount(initialGroups.Count);
        updatedGroups.Should().Contain(ag => ag.Name == "Modified");
    }
}
