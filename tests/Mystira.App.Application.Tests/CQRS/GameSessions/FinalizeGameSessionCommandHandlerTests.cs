using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class FinalizeGameSessionCommandHandlerTests : IDisposable
{
    private readonly ServiceProvider _sp;
    private readonly MystiraAppDbContext _db;
    private readonly FinalizeGameSessionCommandHandler _handler;

    public FinalizeGameSessionCommandHandlerTests()
    {
        var services = new ServiceCollection();

        // In-memory EF Core
        services.AddDbContext<MystiraAppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_Finalize_{Guid.NewGuid()}")
        );
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IPlayerScenarioScoreRepository, PlayerScenarioScoreRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();

        // UoW
        services.AddScoped<IUnitOfWork, Mystira.App.Infrastructure.Data.UnitOfWork.UnitOfWork>();

        // Services
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddScoped<IAxisScoringService, AxisScoringService>();
        services.AddScoped<IBadgeAwardingService, BadgeAwardingService>();

        // Handler
        services.AddScoped<FinalizeGameSessionCommandHandler>();

        _sp = services.BuildServiceProvider();
        _db = _sp.GetRequiredService<MystiraAppDbContext>();
        _handler = _sp.GetRequiredService<FinalizeGameSessionCommandHandler>();
    }

    [Fact]
    public async Task Finalize_TwoSessions_BronzeThenSilverAwarded()
    {
        // Arrange
        var profile = new UserProfile { Id = "profile1", Name = "Player 1", AgeGroupName = "6-9" };

        // Badges: honesty bronze at 0.5, silver at 1.0 for 6-9
        var bronze = new Badge
        {
            Id = "honesty-bronze",
            AgeGroupId = "6-9",
            CompassAxisId = "honesty",
            Tier = "bronze",
            TierOrder = 1,
            Title = "Honesty Bronze",
            Description = "Bronze honesty",
            RequiredScore = 0.5f,
            ImageId = "img-bronze"
        };
        var silver = new Badge
        {
            Id = "honesty-silver",
            AgeGroupId = "6-9",
            CompassAxisId = "honesty",
            Tier = "silver",
            TierOrder = 2,
            Title = "Honesty Silver",
            Description = "Silver honesty",
            RequiredScore = 1.0f,
            ImageId = "img-silver"
        };

        // Session 1: honesty total 0.5 (one choice of 0.5)
        var session1 = new GameSession
        {
            Id = "session-1",
            ProfileId = profile.Id,
            ScenarioId = "scenario-1",
            ChoiceHistory = new List<SessionChoice>
            {
                new() { CompassAxis = "honesty", CompassDelta = 0.5, PlayerId = profile.Id },
            }
        };

        // Session 2: honesty total 0.7 (two choices of 0.3) so per-session qualifies for silver
        var session2 = new GameSession
        {
            Id = "session-2",
            ProfileId = profile.Id,
            ScenarioId = "scenario-2",
            ChoiceHistory = new List<SessionChoice>
            {
                new() { CompassAxis = "honesty", CompassDelta = 0.3, PlayerId = profile.Id },
                new() { CompassAxis = "honesty", CompassDelta = 0.3, PlayerId = profile.Id },
            }
        };

        await _db.UserProfiles.AddAsync(profile);
        await _db.GameSessions.AddRangeAsync(session1, session2);
        await _db.Badges.AddRangeAsync(bronze, silver);
        await _db.SaveChangesAsync();

        // Act & Assert: finalize session 1 → bronze
        var res1 = await _handler.Handle(new FinalizeGameSessionCommand(session1.Id), CancellationToken.None);
        Assert.Equal(session1.Id, res1.SessionId);
        Assert.Single(res1.Awards);
        var awards1 = res1.Awards[0].NewBadges;
        Assert.Single(awards1);
        Assert.Equal("honesty-bronze", awards1[0].BadgeId);

        // Act & Assert: finalize session 2 → silver (bronze already earned, so only silver new)
        var res2 = await _handler.Handle(new FinalizeGameSessionCommand(session2.Id), CancellationToken.None);
        Assert.Equal(session2.Id, res2.SessionId);
        Assert.Single(res2.Awards);
        var awards2 = res2.Awards[0].NewBadges;
        Assert.Single(awards2);
        Assert.Equal("honesty-silver", awards2[0].BadgeId);
    }

    public void Dispose()
    {
        _sp.Dispose();
    }
}
