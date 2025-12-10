using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Application.Ports;
using Mystira.App.Infrastructure.StoryProtocol;
using Mystira.App.Infrastructure.StoryProtocol.Services;
using Xunit;

namespace Mystira.App.Api.Tests.Services;

/// <summary>
/// Tests for Story Protocol service registration
/// </summary>
public class StoryProtocolServiceRegistrationTests
{
    [Fact]
    public void AddStoryProtocolServices_WithNoConfiguration_RegistersMockService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        
        // Add logging (required by MockStoryProtocolService)
        services.AddLogging();

        // Act
        services.AddStoryProtocolServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IStoryProtocolService>();
        Assert.NotNull(service);
        Assert.IsType<MockStoryProtocolService>(service);
    }

    [Fact]
    public void AddStoryProtocolServices_WithEnabledFalse_RegistersMockService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "StoryProtocol:Enabled", "false" },
                { "StoryProtocol:UseMockImplementation", "true" }
            })
            .Build();
        
        // Add logging (required by MockStoryProtocolService)
        services.AddLogging();

        // Act
        services.AddStoryProtocolServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IStoryProtocolService>();
        Assert.NotNull(service);
        Assert.IsType<MockStoryProtocolService>(service);
    }

    [Fact]
    public void AddStoryProtocolServices_WithEnabledTrueAndUseMock_RegistersMockService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "StoryProtocol:Enabled", "true" },
                { "StoryProtocol:UseMockImplementation", "true" }
            })
            .Build();
        
        // Add logging (required by MockStoryProtocolService)
        services.AddLogging();

        // Act
        services.AddStoryProtocolServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IStoryProtocolService>();
        Assert.NotNull(service);
        Assert.IsType<MockStoryProtocolService>(service);
    }

    [Fact]
    public void AddStoryProtocolServices_WithEnabledTrueAndUseMockFalse_RegistersRealService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "StoryProtocol:Enabled", "true" },
                { "StoryProtocol:UseMockImplementation", "false" },
                // Note: This is a fake test private key - not a real wallet
                { "StoryProtocol:PrivateKey", "0x0000000000000000000000000000000000000000000000000000000000000001" }
            })
            .Build();
        
        // Add logging (required by StoryProtocolService)
        services.AddLogging();

        // Act
        services.AddStoryProtocolServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IStoryProtocolService>();
        Assert.NotNull(service);
        Assert.IsType<StoryProtocolService>(service);
    }

    [Fact]
    public void AddStoryProtocolServices_AlwaysRegistersAService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        
        // Add logging
        services.AddLogging();

        // Act
        services.AddStoryProtocolServices(configuration);
        
        // Assert - BuildServiceProvider should not throw
        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IStoryProtocolService>();
        Assert.NotNull(service);
    }
}
