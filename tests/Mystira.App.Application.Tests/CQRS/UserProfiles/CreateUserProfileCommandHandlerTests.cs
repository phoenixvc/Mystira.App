using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.UserProfiles.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.UserProfiles;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.UserProfiles;

public class CreateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<CreateUserProfileCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_WithValidRequest_CreatesProfile()
    {
        var handler = new CreateUserProfileCommandHandler(_repo.Object, _uow.Object, _logger.Object);
        var request = new CreateUserProfileRequest
        {
            Name = "Ava",
            AgeGroup = "6-9",
            PreferredFantasyThemes = new() { "Magic" }
        };

        var result = await handler.Handle(new CreateUserProfileCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Ava");
        result.AgeGroupName.Should().Be("6-9");
        _repo.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Handle_WithMissingName_Throws(string name)
    {
        var handler = new CreateUserProfileCommandHandler(_repo.Object, _uow.Object, _logger.Object);
        var request = new CreateUserProfileRequest
        {
            Name = name,
            AgeGroup = "6-9",
            PreferredFantasyThemes = new() { "Magic" }
        };

        var act = async () => await handler.Handle(new CreateUserProfileCommand(request), CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("Profile name is required");
    }

    [Fact]
    public async Task Handle_WithOneCharacterName_ThrowsWithClearMessage()
    {
        var handler = new CreateUserProfileCommandHandler(_repo.Object, _uow.Object, _logger.Object);
        var request = new CreateUserProfileRequest
        {
            Name = "A",
            AgeGroup = "6-9",
            PreferredFantasyThemes = new() { "Magic" }
        };

        var act = async () => await handler.Handle(new CreateUserProfileCommand(request), CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("Profile name must be at least 2 characters long");
    }

    [Fact]
    public async Task Handle_WithInvalidAgeGroup_Throws()
    {
        var handler = new CreateUserProfileCommandHandler(_repo.Object, _uow.Object, _logger.Object);
        var request = new CreateUserProfileRequest
        {
            Name = "Ava",
            AgeGroup = "invalid",
            PreferredFantasyThemes = new() { "Magic" }
        };

        var act = async () => await handler.Handle(new CreateUserProfileCommand(request), CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("Invalid age group: invalid*");
    }
}
