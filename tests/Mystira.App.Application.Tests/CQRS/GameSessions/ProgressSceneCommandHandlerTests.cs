using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class ProgressSceneCommandHandlerTests : CqrsIntegrationTestBase
{
    private ProgressSceneCommandHandler CreateHandler()
    {
        var repo = ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logger = ServiceProvider.GetRequiredService<ILogger<ProgressSceneCommandHandler>>();
        return new ProgressSceneCommandHandler(repo, uow, logger);
    }

    private async Task<GameSession> SeedSessionAsync(SessionStatus status)
    {
        var db = ServiceProvider.GetRequiredService<MystiraAppDbContext>();
        var s = new GameSession
        {
            Id = Guid.NewGuid().ToString("N"),
            ScenarioId = "scenario-1",
            AccountId = "acc-1",
            ProfileId = "prof-1",
            Status = status,
            StartTime = DateTime.UtcNow
        };
        await db.GameSessions.AddAsync(s);
        await db.SaveChangesAsync();
        return s;
    }

    [Fact]
    public async Task ProgressScene_InProgress_UpdatesCurrentScene()
    {
        var handler = CreateHandler();
        var session = await SeedSessionAsync(SessionStatus.InProgress);

        var req = new ProgressSceneRequest { SessionId = session.Id, SceneId = "scene-2" };
        var updated = await handler.Handle(new ProgressSceneCommand(req), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.CurrentSceneId.Should().Be("scene-2");
    }

    [Fact]
    public async Task ProgressScene_NotInProgress_Throws()
    {
        var handler = CreateHandler();
        var session = await SeedSessionAsync(SessionStatus.Paused);
        var req = new ProgressSceneRequest { SessionId = session.Id, SceneId = "scene-x" };

        var act = async () => await handler.Handle(new ProgressSceneCommand(req), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot progress scene in session with status: *");
    }
}
