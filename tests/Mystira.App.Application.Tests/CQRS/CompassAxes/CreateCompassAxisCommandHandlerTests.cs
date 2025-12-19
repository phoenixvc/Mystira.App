using FluentAssertions;
using Mystira.App.Application.CQRS.CompassAxes.Commands;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.CompassAxes;

public class CreateCompassAxisCommandHandlerTests : CqrsIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesCompassAxis()
    {
        var command = new CreateCompassAxisCommand(
            Name: "Chaos-Order",
            Description: "Represents the spectrum from chaos to order"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Chaos-Order");
        result.Description.Should().Be("Represents the spectrum from chaos to order");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(null)]
    public async Task Handle_WithMissingName_ThrowsArgumentException(string name)
    {
        var command = new CreateCompassAxisCommand(
            Name: name,
            Description: "Test description"
        );

        var act = async () => await Mediator.Send(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name is required");
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_CreatesAxis()
    {
        var command = new CreateCompassAxisCommand(
            Name: "Light-Dark",
            Description: string.Empty
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Name.Should().Be("Light-Dark");
        result.Description.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        var command = new CreateCompassAxisCommand(
            Name: "Good-Evil",
            Description: "Represents morality spectrum"
        );

        await Mediator.Send(command);

        var allAxes = await Mediator.Send(new GetAllCompassAxesQuery());
        allAxes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_CreatedAxisPersistsInDatabase()
    {
        var command = new CreateCompassAxisCommand(
            Name: "Control-Freedom",
            Description: "Control vs Freedom axis"
        );

        var created = await Mediator.Send(command);

        var retrieved = await DbContext.CompassAxes.FindAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved.Name.Should().Be("Control-Freedom");
        retrieved.Description.Should().Be("Control vs Freedom axis");
    }

    [Fact]
    public async Task Handle_WithSpecialCharactersInName_CreatesAxis()
    {
        var command = new CreateCompassAxisCommand(
            Name: "Chaos-Order (v2.0)",
            Description: "Special characters test"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Name.Should().Be("Chaos-Order (v2.0)");
    }
}
