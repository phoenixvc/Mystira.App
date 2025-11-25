using FluentAssertions;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

/// <summary>
/// Tests for ToastService - validates toast notification functionality
/// </summary>
public class ToastServiceTests
{
    [Fact]
    public void ShowSuccess_RaisesOnShowEvent()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;

        // Act
        service.ShowSuccess("Operation successful!");

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Message.Should().Be("Operation successful!");
        capturedMessage.Type.Should().Be(ToastType.Success);
        capturedMessage.DurationMs.Should().Be(3000, "success toasts default to 3 seconds");
    }

    [Fact]
    public void ShowError_RaisesOnShowEvent()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;

        // Act
        service.ShowError("An error occurred");

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Message.Should().Be("An error occurred");
        capturedMessage.Type.Should().Be(ToastType.Error);
        capturedMessage.DurationMs.Should().Be(5000, "error toasts default to 5 seconds");
    }

    [Fact]
    public void ShowWarning_RaisesOnShowEvent()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;

        // Act
        service.ShowWarning("Warning: Check your input");

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Type.Should().Be(ToastType.Warning);
        capturedMessage.DurationMs.Should().Be(4000, "warning toasts default to 4 seconds");
    }

    [Fact]
    public void ShowInfo_RaisesOnShowEvent()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;

        // Act
        service.ShowInfo("Here's some information");

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Type.Should().Be(ToastType.Info);
        capturedMessage.DurationMs.Should().Be(3000, "info toasts default to 3 seconds");
    }

    [Fact]
    public void Show_WithCustomDuration_UsesSpecifiedDuration()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;

        // Act
        service.ShowSuccess("Custom duration message", durationMs: 10000);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.DurationMs.Should().Be(10000);
    }

    [Fact]
    public void Show_SetsUniqueId()
    {
        // Arrange
        var service = new ToastService();
        var messages = new List<ToastMessage>();
        service.OnShow += (message) => messages.Add(message);

        // Act
        service.ShowSuccess("Message 1");
        service.ShowSuccess("Message 2");
        service.ShowSuccess("Message 3");

        // Assert
        messages.Should().HaveCount(3);
        messages.Select(m => m.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Show_SetsTimestamp()
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;
        var beforeShow = DateTime.UtcNow;

        // Act
        service.ShowSuccess("Timestamped message");

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Timestamp.Should().BeAfter(beforeShow);
        capturedMessage.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Clear_RaisesOnClearEvent()
    {
        // Arrange
        var service = new ToastService();
        var clearCalled = false;
        service.OnClear += () => clearCalled = true;

        // Act
        service.Clear();

        // Assert
        clearCalled.Should().BeTrue();
    }

    [Fact]
    public void OnShowEvent_CanHaveMultipleSubscribers()
    {
        // Arrange
        var service = new ToastService();
        var subscriber1Called = false;
        var subscriber2Called = false;

        service.OnShow += (_) => subscriber1Called = true;
        service.OnShow += (_) => subscriber2Called = true;

        // Act
        service.ShowSuccess("Test");

        // Assert
        subscriber1Called.Should().BeTrue();
        subscriber2Called.Should().BeTrue();
    }

    [Theory]
    [InlineData(ToastType.Success, 3000)]
    [InlineData(ToastType.Error, 5000)]
    [InlineData(ToastType.Warning, 4000)]
    [InlineData(ToastType.Info, 3000)]
    public void ShowByType_UsesCorrectDefaultDuration(ToastType type, int expectedDuration)
    {
        // Arrange
        var service = new ToastService();
        ToastMessage? capturedMessage = null;
        service.OnShow += (message) => capturedMessage = message;

        // Act
        switch (type)
        {
            case ToastType.Success:
                service.ShowSuccess("Message");
                break;
            case ToastType.Error:
                service.ShowError("Message");
                break;
            case ToastType.Warning:
                service.ShowWarning("Message");
                break;
            case ToastType.Info:
                service.ShowInfo("Message");
                break;
        }

        // Assert
        capturedMessage!.DurationMs.Should().Be(expectedDuration);
    }
}
