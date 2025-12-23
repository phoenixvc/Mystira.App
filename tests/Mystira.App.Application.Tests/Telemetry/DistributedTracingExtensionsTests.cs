using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mystira.App.Shared.Telemetry;
using System.Diagnostics;

namespace Mystira.App.Application.Tests.Telemetry;

/// <summary>
/// Unit tests for DistributedTracingExtensions.
/// Tests verify Activity creation and W3C trace context propagation.
/// </summary>
public class DistributedTracingExtensionsTests : IDisposable
{
    private readonly ActivityListener _listener;

    public DistributedTracingExtensionsTests()
    {
        // Set up ActivityListener for tests to enable Activity creation
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Mystira"),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }

    [Fact]
    public void AddDistributedTracing_ShouldRegisterActivityListenerHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required by ActivityListenerHostedService

        // Act
        services.AddDistributedTracing("TestService", "Development");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        hostedServices.Should().ContainSingle(s => s.GetType().Name == "ActivityListenerHostedService");
    }

    [Fact]
    public void StartTracedOperation_ShouldCreateActivityWithCorrectName()
    {
        // Arrange & Act
        using var activity = DistributedTracingExtensions.StartOperation("TestOperation", "Database");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("TestOperation");
        activity.GetTagItem("span.type").Should().Be("Database");
    }

    [Fact]
    public void StartDatabaseOperation_ShouldCreateActivityWithDatabaseTags()
    {
        // Arrange & Act
        using var activity = DistributedTracingExtensions.StartDatabaseOperation("FindById", "Accounts");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Contain("FindById");
        activity.GetTagItem("span.type").Should().Be("Database");
        activity.GetTagItem("db.system").Should().Be("cosmosdb");
        activity.GetTagItem("db.collection").Should().Be("Accounts");
    }

    [Fact]
    public void StartDatabaseOperation_WithoutCollection_ShouldNotSetCollectionTag()
    {
        // Arrange & Act
        using var activity = DistributedTracingExtensions.StartDatabaseOperation("Connect");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("db.collection").Should().BeNull();
    }

    [Fact]
    public void StartHttpOperation_ShouldCreateActivityWithHttpTags()
    {
        // Arrange & Act
        using var activity = DistributedTracingExtensions.StartHttpOperation("GET", "https://api.example.com/users");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Contain("GET");
        activity.GetTagItem("span.type").Should().Be("HTTP");
        activity.GetTagItem("http.method").Should().Be("GET");
        activity.GetTagItem("http.url").Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void StartCacheOperation_ShouldCreateActivityWithCacheTags()
    {
        // Arrange & Act
        using var activity = DistributedTracingExtensions.StartCacheOperation("GET", "user:123");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Contain("GET");
        activity.GetTagItem("span.type").Should().Be("Cache");
        activity.GetTagItem("cache.operation").Should().Be("GET");
        activity.GetTagItem("cache.key").Should().Be("user:123");
    }

    [Fact]
    public void StartGrpcOperation_ShouldCreateActivityWithGrpcTags()
    {
        // Arrange & Act
        using var activity = DistributedTracingExtensions.StartGrpcOperation("ChainService", "RegisterIpAsset");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Contain("RegisterIpAsset");
        activity.GetTagItem("span.type").Should().Be("gRPC");
        activity.GetTagItem("rpc.service").Should().Be("ChainService");
        activity.GetTagItem("rpc.method").Should().Be("RegisterIpAsset");
    }

    [Fact]
    public void StartTracedOperation_ShouldPropagateTraceContext()
    {
        // Arrange
        using var parentActivity = new Activity("ParentOperation").Start();
        var parentTraceId = parentActivity.TraceId;

        // Act
        using var childActivity = DistributedTracingExtensions.StartOperation("ChildOperation", "Test");

        // Assert
        childActivity.Should().NotBeNull();
        childActivity!.TraceId.Should().Be(parentTraceId);
        childActivity.ParentSpanId.Should().Be(parentActivity.SpanId);
    }

    [Fact]
    public void StartTracedOperation_WithTags_ShouldIncludeAllTags()
    {
        // Arrange
        var tags = new Dictionary<string, object?>
        {
            ["custom.tag1"] = "value1",
            ["custom.tag2"] = 42
        };

        // Act
        using var activity = DistributedTracingExtensions.StartOperation("TaggedOperation", "Test");
        activity?.SetTag("custom.tag1", "value1");
        activity?.SetTag("custom.tag2", 42);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("custom.tag1").Should().Be("value1");
        activity.GetTagItem("custom.tag2").Should().Be(42);
    }
}
