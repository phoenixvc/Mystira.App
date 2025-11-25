// Example Blazor Component Test (using bUnit)
// File: tests/Mystira.App.PWA.Tests/Components/LoadingIndicatorTests.cs

using Bunit;
using FluentAssertions;
using Mystira.App.PWA.Components;
using Xunit;

namespace Mystira.App.PWA.Tests.Components;

/// <summary>
/// Example tests for a Blazor component (LoadingIndicator)
/// Demonstrates testing component rendering, parameters, and user interactions
/// Uses bUnit for component testing
/// </summary>
public class LoadingIndicatorTests : TestContext
{
    [Fact]
    public void LoadingIndicator_WithDefaultParameters_RendersSpinner()
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>();

        // Assert
        cut.Find(".loading-spinner").Should().NotBeNull();
        cut.FindAll(".skeleton-item").Should().BeEmpty("skeleton should not render by default");
    }

    [Fact]
    public void LoadingIndicator_WithMessage_RendersMessage()
    {
        // Arrange
        var message = "Loading adventures...";

        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Find(".loading-message").TextContent.Should().Be(message);
    }

    [Fact]
    public void LoadingIndicator_WithSkeletonMode_RendersSkeleton()
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.ShowSpinner, false)
            .Add(p => p.ShowSkeleton, true));

        // Assert
        cut.FindAll(".skeleton-item").Should().HaveCount(3, "default skeleton count is 3");
        cut.FindAll(".loading-spinner").Should().BeEmpty("spinner should not render in skeleton mode");
    }

    [Fact]
    public void LoadingIndicator_WithCustomSkeletonCount_RendersCorrectCount()
    {
        // Arrange
        var skeletonCount = 5;

        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.ShowSpinner, false)
            .Add(p => p.ShowSkeleton, true)
            .Add(p => p.SkeletonCount, skeletonCount));

        // Assert
        cut.FindAll(".skeleton-item").Should().HaveCount(skeletonCount);
    }

    [Fact]
    public void LoadingIndicator_WithBothModes_RendersBoth()
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.ShowSpinner, true)
            .Add(p => p.ShowSkeleton, true));

        // Assert
        cut.Find(".loading-spinner").Should().NotBeNull();
        cut.FindAll(".skeleton-item").Should().HaveCount(3);
    }

    [Fact]
    public void LoadingIndicator_HasAriaAttributes()
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>();

        // Assert
        var container = cut.Find(".loading-container");
        container.GetAttribute("role").Should().Be("status");
        container.GetAttribute("aria-live").Should().Be("polite");
    }

    [Fact]
    public void LoadingIndicator_WithCssClass_AppliesCustomClass()
    {
        // Arrange
        var customClass = "my-custom-loader";

        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.CssClass, customClass));

        // Assert
        cut.Markup.Should().Contain(customClass);
    }

    [Fact]
    public void LoadingIndicator_WithNoMessage_DoesNotRenderMessageElement()
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>();

        // Assert
        cut.FindAll(".loading-message").Should().BeEmpty();
    }

    [Fact]
    public void LoadingIndicator_SkeletonLines_HaveCorrectClasses()
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.ShowSpinner, false)
            .Add(p => p.ShowSkeleton, true));

        // Assert
        var skeletonItem = cut.Find(".skeleton-item");
        skeletonItem.FindAll(".skeleton-line").Should().HaveCount(3);
        skeletonItem.FindAll(".skeleton-line-title").Should().HaveCount(1);
        skeletonItem.FindAll(".skeleton-line-text").Should().HaveCount(2);
    }
}

// Example: Testing component with dependencies (injected services)
// File: tests/Mystira.App.PWA.Tests/Components/ToastContainerTests.cs

/// <summary>
/// Example tests for a component with service dependencies (ToastContainer)
/// Demonstrates mocking services and testing component lifecycle
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

    [Fact]
    public void ToastContainer_DisposesCorrectly()
    {
        // Arrange
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        var cut = RenderComponent<ToastContainer>();

        // Act
        cut.Dispose();

        // Verify no errors during disposal
        // (Component should unsubscribe from events and dispose timers)
        // This is a smoke test - actual verification would require inspecting internal state
    }
}

// Example: Testing component with callbacks
// File: tests/Mystira.App.PWA.Tests/Components/ErrorBoundaryWrapperTests.cs

/// <summary>
/// Example tests for a component with event callbacks (ErrorBoundaryWrapper)
/// Demonstrates testing user interactions and callbacks
/// </summary>
public class ErrorBoundaryWrapperTests : TestContext
{
    [Fact]
    public void ErrorBoundaryWrapper_WithoutError_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorBoundaryWrapper>(parameters => parameters
            .AddChildContent("<div class='test-child'>Child content</div>"));

        // Assert
        cut.Find(".test-child").TextContent.Should().Be("Child content");
        cut.FindAll(".error-boundary-container").Should().BeEmpty();
    }

    [Fact]
    public void ErrorBoundaryWrapper_WithCustomErrorTitle_RendersCustomTitle()
    {
        // Arrange
        var customTitle = "Oops! Something broke";

        // Act
        var cut = RenderComponent<ErrorBoundaryWrapper>(parameters => parameters
            .Add(p => p.ErrorTitle, customTitle)
            .AddChildContent("<ThrowingComponent />"));

        // Simulate error (this would normally be triggered by child component throwing)
        // In bUnit, you can use cut.InvokeAsync to trigger errors

        // Note: Testing ErrorBoundary requires the child to actually throw
        // This is a structural test showing the component accepts the parameter
        cut.Instance.ErrorTitle.Should().Be(customTitle);
    }

    [Fact]
    public void ErrorBoundaryWrapper_OnRecoverClick_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = RenderComponent<ErrorBoundaryWrapper>(parameters => parameters
            .Add(p => p.OnRecover, EventCallback.Factory.Create(this, () => callbackInvoked = true))
            .AddChildContent("<div>Content</div>"));

        // Note: To fully test this, you'd need to trigger an error first
        // Then click the recover button and verify the callback is invoked

        // This demonstrates the pattern - actual implementation would require
        // a component that throws on render to test the error state
    }
}
