using DMfinity.Domain.Models;
using FluentAssertions;
using Xunit;

namespace DMfinity.Domain.Tests.Models;

public class GameSessionTests
{
    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenNotPaused()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            Status = SessionStatus.InProgress
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().BeCloseTo(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenPaused()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            Status = SessionStatus.InProgress,
            IsPaused = true,
            PausedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenFinished()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            Status = SessionStatus.Completed,
            ElapsedTime = TimeSpan.FromMinutes(10)
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().Be(TimeSpan.FromMinutes(10));
    }
}