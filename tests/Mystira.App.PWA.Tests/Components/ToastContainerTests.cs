using Bunit;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Mystira.App.PWA.Components;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Components;

/// <summary>
/// Tests for ToastContainer component - validates toast rendering and user interactions
/// </summary>
public class ToastContainerTests : TestContext
{
    [Fact]
    public void ToastContainer_OnInitialized_SubscribesToToastService()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Show a toast
        toastService.ShowSuccess("Test message");
        cut.WaitForState(() => cut.FindAll(".toast").Count == 1);

        // Assert
        cut.Find(".toast").Should().NotBeNull();
        cut.Find(".toast-message").TextContent.Should().Be("Test message");
    }

    [Fact]
    public void ToastContainer_OnToastShow_RendersToast()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        // Act
        toastService.ShowSuccess("Success message");
        cut.WaitForState(() => cut.FindAll(".toast").Count > 0);

        // Assert
        var toast = cut.Find(".toast");
        toast.ClassList.Should().Contain("toast-success");
        cut.Find(".toast-message").TextContent.Should().Be("Success message");
    }

    [Fact]
    public void ToastContainer_OnCloseButtonClick_RemovesToast()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        toastService.ShowSuccess("Message to close");
        cut.WaitForState(() => cut.FindAll(".toast").Count == 1);

        // Act
        var closeButton = cut.Find(".toast-close");
        closeButton.Click();

        // Assert
        cut.WaitForState(() => cut.FindAll(".toast").Count == 0, TimeSpan.FromSeconds(1));
        cut.FindAll(".toast").Should().BeEmpty();
    }

    [Fact]
    public void ToastContainer_WithMultipleToasts_RendersAll()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        // Act
        toastService.ShowSuccess("Message 1");
        toastService.ShowError("Message 2");
        toastService.ShowWarning("Message 3");
        cut.WaitForState(() => cut.FindAll(".toast").Count == 3);

        // Assert
        cut.FindAll(".toast").Should().HaveCount(3);
        cut.FindAll(".toast-success").Should().HaveCount(1);
        cut.FindAll(".toast-error").Should().HaveCount(1);
        cut.FindAll(".toast-warning").Should().HaveCount(1);
    }

    [Fact]
    public void ToastContainer_OnClear_RemovesAllToasts()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        toastService.ShowSuccess("Message 1");
        toastService.ShowError("Message 2");
        cut.WaitForState(() => cut.FindAll(".toast").Count == 2);

        // Act
        toastService.Clear();

        // Assert
        cut.WaitForState(() => cut.FindAll(".toast").Count == 0, TimeSpan.FromSeconds(1));
        cut.FindAll(".toast").Should().BeEmpty();
    }

    [Fact]
    public void ToastContainer_HasCorrectAriaAttributes()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        // Act
        var cut = RenderComponent<ToastContainer>();

        // Assert
        var container = cut.Find(".toast-container");
        container.GetAttribute("aria-live").Should().Be("polite");
        container.GetAttribute("aria-atomic").Should().Be("true");
    }

    [Fact]
    public void ToastContainer_ToastCloseButton_HasAriaLabel()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        // Act
        toastService.ShowSuccess("Test");
        cut.WaitForState(() => cut.FindAll(".toast").Count == 1);

        // Assert
        var closeButton = cut.Find(".toast-close");
        closeButton.GetAttribute("aria-label").Should().Be("Close notification");
    }

    [Theory]
    [InlineData(ToastType.Success, "toast-success", "✓")]
    [InlineData(ToastType.Error, "toast-error", "✕")]
    [InlineData(ToastType.Warning, "toast-warning", "⚠")]
    [InlineData(ToastType.Info, "toast-info", "ℹ")]
    public void ToastContainer_WithDifferentTypes_RendersCorrectIcon(ToastType type, string expectedClass, string expectedIcon)
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        // Act
        toastService.Show("Test message", type);
        cut.WaitForState(() => cut.FindAll(".toast").Count == 1);

        // Assert
        var toast = cut.Find(".toast");
        toast.ClassList.Should().Contain(expectedClass);
        cut.Find(".toast-icon").TextContent.Trim().Should().Be(expectedIcon);
    }

    [Fact]
    public void ToastContainer_DisposesCorrectly()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        toastService.ShowSuccess("Test 1");
        toastService.ShowError("Test 2");
        cut.WaitForState(() => cut.FindAll(".toast").Count == 2);

        // Act - Dispose should clean up timers and unsubscribe from events
        cut.Dispose();

        // Verify no errors during disposal (timers disposed, events unsubscribed)
        // This is a smoke test - actual verification would require inspecting internal state
    }
}
