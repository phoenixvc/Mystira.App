using FluentAssertions;
using Mystira.App.Application.CQRS.AgeGroups.Commands;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.AgeGroups;

public class CreateAgeGroupCommandHandlerTests : CqrsIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesAgeGroup()
    {
        var command = new CreateAgeGroupCommand(
            Name: "Children",
            Value: "5-8",
            MinimumAge: 5,
            MaximumAge: 8,
            Description: "For young children"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Children");
        result.Value.Should().Be("5-8");
        result.MinimumAge.Should().Be(5);
        result.MaximumAge.Should().Be(8);
        result.Description.Should().Be("For young children");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Handle_WithMissingName_ThrowsArgumentException(string name)
    {
        var command = new CreateAgeGroupCommand(
            Name: name,
            Value: "5-8",
            MinimumAge: 5,
            MaximumAge: 8,
            Description: "Test"
        );

        var act = async () => await Mediator.Send(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Handle_WithMissingValue_ThrowsArgumentException(string value)
    {
        var command = new CreateAgeGroupCommand(
            Name: "Children",
            Value: value,
            MinimumAge: 5,
            MaximumAge: 8,
            Description: "Test"
        );

        var act = async () => await Mediator.Send(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Value is required");
    }

    [Fact]
    public async Task Handle_WithValidCommand_InvalidatesCachePrefix()
    {
        var command = new CreateAgeGroupCommand(
            Name: "Teens",
            Value: "13-18",
            MinimumAge: 13,
            MaximumAge: 18,
            Description: "For teenagers"
        );

        await Mediator.Send(command);

        var allAgeGroups = await Mediator.Send(new GetAllAgeGroupsQuery());
        allAgeGroups.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_CreatedAgeGroupPersistsInDatabase()
    {
        var command = new CreateAgeGroupCommand(
            Name: "Adults",
            Value: "18+",
            MinimumAge: 18,
            MaximumAge: 100,
            Description: "For adults"
        );

        var created = await Mediator.Send(command);

        var retrieved = await DbContext.AgeGroupDefinitions.FindAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Adults");
        retrieved.Value.Should().Be("18+");
    }
}
