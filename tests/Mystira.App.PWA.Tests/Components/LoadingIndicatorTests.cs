using Bunit;
using FluentAssertions;
using Mystira.App.PWA.Components;
using Xunit;

namespace Mystira.App.PWA.Tests.Components;

/// <summary>
/// Tests for LoadingIndicator component - validates loading states and skeleton rendering
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
        skeletonItem.QuerySelectorAll(".skeleton-line").Length.Should().Be(3);
        skeletonItem.QuerySelectorAll(".skeleton-line-title").Length.Should().Be(1);
        skeletonItem.QuerySelectorAll(".skeleton-line-text").Length.Should().Be(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void LoadingIndicator_WithVariousSkeletonCounts_RendersCorrectly(int count)
    {
        // Act
        var cut = RenderComponent<LoadingIndicator>(parameters => parameters
            .Add(p => p.ShowSpinner, false)
            .Add(p => p.ShowSkeleton, true)
            .Add(p => p.SkeletonCount, count));

        // Assert
        cut.FindAll(".skeleton-item").Should().HaveCount(count);
    }
}
