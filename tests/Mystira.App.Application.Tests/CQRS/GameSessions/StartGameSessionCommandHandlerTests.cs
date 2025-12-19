using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Models.GameSessions;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class StartGameSessionCommandHandlerTests : CqrsIntegrationTestBase
{
    private StartGameSessionCommandHandler CreateHandler()
    {
        var repo = ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logger = ServiceProvider.GetRequiredService<ILogger<StartGameSessionCommandHandler>>();
        return new StartGameSessionCommandHandler(repo, uow, logger);
    }

    [Fact]
    public async Task Start_WithPlayerNames_SucceedsAndPersists()
    {
        var handler = CreateHandler();

        var req = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "acc-1",
            ProfileId = "prof-1",
            PlayerNames = new() { "Ava" },
            TargetAgeGroup = "6-9"
        };

        var session = await handler.Handle(new StartGameSessionCommand(req), CancellationToken.None);

        session.Should().NotBeNull();
        session.Status.Should().Be(SessionStatus.InProgress);
        session.PlayerNames.Should().ContainSingle().Which.Should().Be("Ava");
        session.ScenarioId.Should().Be("scenario-1");

        // persisted
        var db = ServiceProvider.GetRequiredService<MystiraAppDbContext>();
        var loaded = await db.GameSessions.FindAsync(session.Id);
        loaded.Should().NotBeNull();
        loaded!.AccountId.Should().Be("acc-1");
    }

    [Fact]
    public async Task Start_WithCharacterAssignments_DerivesPlayerNames()
    {
        var handler = CreateHandler();

        var req = new StartGameSessionRequest
        {
            ScenarioId = "scenario-2",
            AccountId = "acc-1",
            ProfileId = "prof-2",
            TargetAgeGroup = "6-9",
            CharacterAssignments =
            {
                new CharacterAssignmentDto
                {
                    CharacterId = "c1",
                    CharacterName = "Hero",
                    Role = "Leader",
                    Archetype = "Brave",
                    IsUnused = false,
                    PlayerAssignment = new PlayerAssignmentDto { Type = "Player", ProfileName = "Ben" }
                },
                new CharacterAssignmentDto
                {
                    CharacterId = "c2",
                    CharacterName = "Mage",
                    Role = "Support",
                    Archetype = "Wise",
                    IsUnused = false,
                    PlayerAssignment = new PlayerAssignmentDto { Type = "Guest", GuestName = "Cara" }
                },
                new CharacterAssignmentDto
                {
                    CharacterId = "c3",
                    CharacterName = "Unused",
                    Role = "",
                    Archetype = "",
                    IsUnused = true
                }
            }
        };

        var session = await handler.Handle(new StartGameSessionCommand(req), CancellationToken.None);

        session.PlayerNames.Should().BeEquivalentTo(new[] { "Ben", "Cara" });
        session.CharacterAssignments.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(null, "acc", "prof", "6-9")]
    [InlineData("", "acc", "prof", "6-9")]
    [InlineData("scenario", null, "prof", "6-9")]
    [InlineData("scenario", "", "prof", "6-9")]
    [InlineData("scenario", "acc", null, "6-9")]
    [InlineData("scenario", "acc", "", "6-9")]
    public async Task Start_MissingRequiredFields_Throws(string? scenarioId, string? accountId, string? profileId, string age)
    {
        var handler = CreateHandler();
        var req = new StartGameSessionRequest
        {
            ScenarioId = scenarioId ?? string.Empty,
            AccountId = accountId ?? string.Empty,
            ProfileId = profileId ?? string.Empty,
            TargetAgeGroup = age,
            PlayerNames = new() { "A" }
        };

        var act = async () => await handler.Handle(new StartGameSessionCommand(req), CancellationToken.None);

        if (string.IsNullOrEmpty(scenarioId))
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("ScenarioId is required");
            return;
        }
        if (string.IsNullOrEmpty(accountId))
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("AccountId is required");
            return;
        }
        if (string.IsNullOrEmpty(profileId))
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("ProfileId is required");
            return;
        }
    }

    [Fact]
    public async Task Start_NoPlayersOrAssignments_Throws()
    {
        var handler = CreateHandler();
        var req = new StartGameSessionRequest
        {
            ScenarioId = "scenario",
            AccountId = "acc",
            ProfileId = "prof",
            TargetAgeGroup = "6-9"
        };

        var act = async () => await handler.Handle(new StartGameSessionCommand(req), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("At least one player or character assignment is required");
    }
}
