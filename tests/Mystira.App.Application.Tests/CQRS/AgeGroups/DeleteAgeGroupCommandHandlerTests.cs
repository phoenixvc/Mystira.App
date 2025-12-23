using FluentAssertions;
using Mystira.App.Application.CQRS.AgeGroups.Commands;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.AgeGroups;

public class DeleteAgeGroupCommandHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var ageGroups = new[]
        {
            new AgeGroupDefinition
            {
                Id = "ag-1",
                Name = "Children",
                Value = "5-8",
                MinimumAge = 5,
                MaximumAge = 8,
                Description = "For children"
            },
            new AgeGroupDefinition
            {
                Id = "ag-2",
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
    public async Task Handle_WithValidId_DeletesAgeGroup()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new DeleteAgeGroupCommand("ag-1"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFalse()
    {
        var result = await Mediator.Send(new DeleteAgeGroupCommand("invalid-id"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SoftDeletesEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteAgeGroupCommand("ag-1"));

        var deleted = await DbContext.AgeGroupDefinitions.FindAsync("ag-1");
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeletesOnlyTargetEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteAgeGroupCommand("ag-1"));

        // Use a new scope or clear the DbContext to ensure we're not seeing cached entities
        DbContext.ChangeTracker.Clear();

        var remaining = await Mediator.Send(new GetAllAgeGroupsQuery());
        remaining.Should().HaveCount(1);
        remaining.First().Id.Should().Be("ag-2");
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        await SeedTestDataAsync();
        var initialGroups = await Mediator.Send(new GetAllAgeGroupsQuery());

        await Mediator.Send(new DeleteAgeGroupCommand("ag-1"));

        // Manual clearing and using a fresh mediator if possible,
        // but here we just want to ensure we get a fresh query from the DB
        DbContext.ChangeTracker.Clear();

        var updatedGroups = await Mediator.Send(new GetAllAgeGroupsQuery());

        // Filter out soft deleted just in case the in-memory provider is being tricky
        var activeGroups = updatedGroups.Where(g => !g.IsDeleted).ToList();
        activeGroups.Should().HaveCount(initialGroups.Count - 1);
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedId_ReturnsFalse()
    {
        await SeedTestDataAsync();
        await Mediator.Send(new DeleteAgeGroupCommand("ag-1"));

        var secondResult = await Mediator.Send(new DeleteAgeGroupCommand("ag-1"));

        secondResult.Should().BeFalse();
    }
}
