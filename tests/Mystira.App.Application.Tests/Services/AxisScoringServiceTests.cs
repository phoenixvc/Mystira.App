using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.Tests.Services;

public class AxisScoringServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MystiraAppDbContext _dbContext;
    private readonly IAxisScoringService _scoringService;
    private readonly IPlayerScenarioScoreRepository _scoreRepository;

    public AxisScoringServiceTests()
    {
        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<MystiraAppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

        // Add repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPlayerScenarioScoreRepository, PlayerScenarioScoreRepository>();

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, Mystira.App.Infrastructure.Data.UnitOfWork.UnitOfWork>();

        // Add logging
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Add the service to test
        services.AddScoped<IAxisScoringService, AxisScoringService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<MystiraAppDbContext>();
        _scoringService = _serviceProvider.GetRequiredService<IAxisScoringService>();
        _scoreRepository = _serviceProvider.GetRequiredService<IPlayerScenarioScoreRepository>();
    }

    [Fact]
    public async Task ScoreSessionAsync_WithValidSession_CreatesPlayerScenarioScore()
    {
        // Arrange
        var profile = new UserProfile { Id = "profile1", Name = "Test Player" };
        var session = new GameSession
        {
            Id = "session1",
            ProfileId = "profile1",
            ScenarioId = "scenario1",
            ChoiceHistory = new List<SessionChoice>
            {
                new() { CompassAxis = "honesty", CompassDelta = 5.0, PlayerId = "profile1" },
                new() { CompassAxis = "honesty", CompassDelta = 3.0, PlayerId = "profile1" },
                new() { CompassAxis = "bravery", CompassDelta = -2.0, PlayerId = "profile1" }
            }
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.GameSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _scoringService.ScoreSessionAsync(session, profile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.ProfileId);
        Assert.Equal("scenario1", result.ScenarioId);
        Assert.Equal("session1", result.GameSessionId);
        Assert.Equal(8f, result.AxisScores["honesty"], 0.01f);
        Assert.Equal(-2f, result.AxisScores["bravery"], 0.01f);
    }

    [Fact]
    public async Task ScoreSessionAsync_WithDuplicateScenario_ReturnsNull()
    {
        // Arrange
        var profile = new UserProfile { Id = "profile1", Name = "Test Player" };
        var session = new GameSession
        {
            Id = "session1",
            ProfileId = "profile1",
            ScenarioId = "scenario1",
            ChoiceHistory = new List<SessionChoice>
            {
                new() { CompassAxis = "honesty", CompassDelta = 5.0, PlayerId = "profile1" }
            }
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.GameSessions.AddAsync(session);

        // Create existing score
        var existingScore = new PlayerScenarioScore
        {
            ProfileId = "profile1",
            ScenarioId = "scenario1",
            GameSessionId = "session1",
            AxisScores = new Dictionary<string, float> { { "honesty", 5f } }
        };
        await _dbContext.PlayerScenarioScores.AddAsync(existingScore);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _scoringService.ScoreSessionAsync(session, profile);

        // Assert
        Assert.Null(result);

        // Verify only one score exists
        var allScores = await _scoreRepository.GetByProfileIdAsync(profile.Id);
        Assert.Single(allScores);
    }

    [Fact]
    public async Task ScoreSessionAsync_WithNoCompassChoices_CreatesEmptyScore()
    {
        // Arrange
        var profile = new UserProfile { Id = "profile1", Name = "Test Player" };
        var session = new GameSession
        {
            Id = "session1",
            ProfileId = "profile1",
            ScenarioId = "scenario1",
            ChoiceHistory = new List<SessionChoice>
            {
                new() { CompassAxis = null }, // No axis
                new() { CompassDelta = null } // No delta
            }
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.GameSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _scoringService.ScoreSessionAsync(session, profile);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.AxisScores);
    }

    [Fact]
    public async Task ScoreSessionAsync_PersistsToRepository()
    {
        // Arrange
        var profile = new UserProfile { Id = "profile1", Name = "Test Player" };
        var session = new GameSession
        {
            Id = "session1",
            ProfileId = "profile1",
            ScenarioId = "scenario1",
            ChoiceHistory = new List<SessionChoice>
            {
                new() { CompassAxis = "honesty", CompassDelta = 10.0, PlayerId = "profile1"}
            },
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.GameSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        // Act
        await _scoringService.ScoreSessionAsync(session, profile);

        // Assert - verify it can be retrieved
        var retrieved = await _scoreRepository.GetByProfileAndScenarioAsync("profile1", "scenario1");
        Assert.NotNull(retrieved);
        Assert.Equal(10f, retrieved.AxisScores["honesty"]);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
