using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class PauseResumeEndGameSessionCommandHandlerTests : CqrsIntegrationTestBase
{
    private (PauseGameSessionCommandHandler pause, ResumeGameSessionCommandHandler resume, EndGameSessionCommandHandler end) CreateHandlers()
    {
        var repo = ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = ServiceProvider.GetRequiredService<IUnitOfWork>();
        var lp = ServiceProvider.GetRequiredService<ILogger<PauseGameSessionCommandHandler>>();
        var lr = ServiceProvider.GetRequiredService<ILogger<ResumeGameSessionCommandHandler>>();
        var le = ServiceProvider.GetRequiredService<ILogger<EndGameSessionCommandHandler>>();
        return (new PauseGameSessionCommandHandler(repo, uow, lp),
                new ResumeGameSessionCommandHandler(repo, uow, lr),
                new EndGameSessionCommandHandler(repo, uow, le));
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
    public async Task Pause_Then_Resume_TransitionsStatus()
    {
        var (pause, resume, _) = CreateHandlers();
        var session = await SeedSessionAsync(SessionStatus.InProgress);

        var paused = await pause.Handle(new PauseGameSessionCommand(session.Id), CancellationToken.None);
        paused.Should().NotBeNull();
        paused!.Status.Should().Be(SessionStatus.Paused);
        paused.IsPaused.Should().BeTrue();
        paused.PausedAt.Should().NotBeNull();

        var resumed = await resume.Handle(new ResumeGameSessionCommand(session.Id), CancellationToken.None);
        resumed.Should().NotBeNull();
        resumed!.Status.Should().Be(SessionStatus.InProgress);
        resumed.IsPaused.Should().BeFalse();
        resumed.PausedAt.Should().BeNull();
    }

    [Fact]
    public async Task Pause_WhenNotInProgress_ReturnsNull()
    {
        var (pause, _, __) = CreateHandlers();
        var session = await SeedSessionAsync(SessionStatus.Completed);

        var result = await pause.Handle(new PauseGameSessionCommand(session.Id), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task End_SetsCompletedAndEndTime()
    {
        var (_, _, end) = CreateHandlers();
        var session = await SeedSessionAsync(SessionStatus.InProgress);

        var ended = await end.Handle(new EndGameSessionCommand(session.Id), CancellationToken.None);
        ended.Should().NotBeNull();
        ended!.Status.Should().Be(SessionStatus.Completed);
        ended.EndTime.Should().NotBeNull();
    }
}
