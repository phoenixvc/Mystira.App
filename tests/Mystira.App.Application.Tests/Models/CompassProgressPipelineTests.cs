using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Contracts.Requests.GameSessions;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.Models;

public class CompassProgressPipelineTests
{
    [Fact]
    public void GameSession_RecalculateCompassProgressFromHistory_ComputesPerPlayerAndAxisTotals()
    {
        var session = new GameSession
        {
            Id = "s1",
            AccountId = "a1",
            ProfileId = "owner",
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        session.ChoiceHistory.AddRange([
            new SessionChoice
            {
                SceneId = "scene1",
                ChoiceText = "A",
                NextScene = "scene2",
                PlayerId = "p1",
                CompassAxis = "honesty",
                CompassDirection = "positive",
                CompassDelta = 1.0
            },
            new SessionChoice
            {
                SceneId = "scene2",
                ChoiceText = "B",
                NextScene = "scene3",
                PlayerId = "p1",
                CompassAxis = "honesty",
                CompassDirection = "negative",
                CompassDelta = 0.5
            },
            new SessionChoice
            {
                SceneId = "scene3",
                ChoiceText = "C",
                NextScene = "scene4",
                CompassChange = new CompassChange { Axis = "honesty", Delta = 0.7 }
            },
            new SessionChoice
            {
                SceneId = "scene4",
                ChoiceText = "D",
                NextScene = "scene5",
                PlayerId = "p2",
                CompassAxis = "kindness",
                CompassDirection = "positive",
                CompassDelta = 10.0
            }
        ]);

        session.RecalculateCompassProgressFromHistory();

        session.PlayerCompassProgressTotals.Should().ContainEquivalentOf(new PlayerCompassProgress
        {
            PlayerId = "p1",
            Axis = "honesty",
            Total = 0.5
        });

        session.PlayerCompassProgressTotals.Should().ContainEquivalentOf(new PlayerCompassProgress
        {
            PlayerId = "owner",
            Axis = "honesty",
            Total = 0.7
        });

        session.PlayerCompassProgressTotals.Should().ContainEquivalentOf(new PlayerCompassProgress
        {
            PlayerId = "p2",
            Axis = "kindness",
            Total = 2.0
        });

        session.CompassValues["honesty"].CurrentValue.Should().BeApproximately(1.2, 0.0001);
        session.CompassValues["kindness"].CurrentValue.Should().BeApproximately(2.0, 0.0001);
    }

    [Fact]
    public async Task MystiraAppDbContext_Persists_ChoiceCompassFields_And_PlayerTotals()
    {
        var options = new DbContextOptionsBuilder<MystiraAppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new MystiraAppDbContext(options);

        var session = new GameSession
        {
            Id = "s1",
            AccountId = "a1",
            ProfileId = "owner",
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        session.ChoiceHistory.Add(new SessionChoice
        {
            SceneId = "scene1",
            ChoiceText = "A",
            NextScene = "scene2",
            PlayerId = "p1",
            CompassAxis = "honesty",
            CompassDirection = "positive",
            CompassDelta = 1.0
        });

        session.RecalculateCompassProgressFromHistory();

        context.GameSessions.Add(session);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.GameSessions.SingleAsync(s => s.Id == "s1");

        loaded.ChoiceHistory.Should().HaveCount(1);
        loaded.ChoiceHistory[0].PlayerId.Should().Be("p1");
        loaded.ChoiceHistory[0].CompassAxis.Should().Be("honesty");
        loaded.ChoiceHistory[0].CompassDirection.Should().Be("positive");
        loaded.ChoiceHistory[0].CompassDelta.Should().Be(1.0);

        loaded.PlayerCompassProgressTotals.Should().ContainSingle(p => p.PlayerId == "p1" && p.Axis == "honesty" && p.Total == 1.0);
    }

    [Fact]
    public void MakeChoiceRequest_SerializesAndDeserializes_PlayerAndCompassFields()
    {
        var req = new MakeChoiceRequest
        {
            SessionId = "s1",
            SceneId = "scene1",
            ChoiceText = "A",
            NextSceneId = "scene2",
            PlayerId = "p1",
            CompassAxis = "honesty",
            CompassDirection = "negative",
            CompassDelta = 0.5
        };

        var json = JsonSerializer.Serialize(req, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.Should().Contain("\"playerId\"");
        json.Should().Contain("\"compassAxis\"");

        var roundTrip = JsonSerializer.Deserialize<MakeChoiceRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        roundTrip.Should().NotBeNull();
        roundTrip!.PlayerId.Should().Be("p1");
        roundTrip.CompassAxis.Should().Be("honesty");
        roundTrip.CompassDirection.Should().Be("negative");
        roundTrip.CompassDelta.Should().Be(0.5);
    }
}
