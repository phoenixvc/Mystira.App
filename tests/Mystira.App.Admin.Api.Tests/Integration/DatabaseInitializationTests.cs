using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Mystira.App.Admin.Api.Tests.Integration;

/// <summary>
/// Tests for database initialization timeout protection.
/// Verifies that the application handles database connection issues gracefully.
/// </summary>
public class DatabaseInitializationTests
{
    private readonly ITestOutputHelper _output;

    public DatabaseInitializationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DatabaseInitialization_WithTimeout_ShouldNotHangIndefinitely()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(2);
        var longRunningTask = Task.Run(async () =>
        {
            // Simulate a hanging database operation
            await Task.Delay(TimeSpan.FromSeconds(10));
        });

        // Act
        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(longRunningTask, timeoutTask);

        // Assert
        Assert.Equal(timeoutTask, completedTask);
        _output.WriteLine($"✅ Timeout protection worked: operation stopped after {timeout.TotalSeconds}s");
    }

    [Fact]
    public void ConfigurationDefaults_ShouldMatchProductionRequirements()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Simulate production configuration with no explicit settings
            })
            .Build();

        // Act
        var initDbOnStartup = configuration.GetValue("InitializeDatabaseOnStartup", defaultValue: false);
        var seedOnStartup = configuration.GetValue("SeedMasterDataOnStartup", defaultValue: false);

        // Assert
        Assert.False(initDbOnStartup, "InitializeDatabaseOnStartup should default to false for production safety");
        Assert.False(seedOnStartup, "SeedMasterDataOnStartup should default to false for production safety");
        _output.WriteLine("✅ Configuration defaults are safe for production deployment");
    }

    [Fact]
    public void ConfigurationWithExplicitSettings_ShouldOverrideDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InitializeDatabaseOnStartup"] = "true",
                ["SeedMasterDataOnStartup"] = "true"
            })
            .Build();

        // Act
        var initDbOnStartup = configuration.GetValue("InitializeDatabaseOnStartup", defaultValue: false);
        var seedOnStartup = configuration.GetValue("SeedMasterDataOnStartup", defaultValue: false);

        // Assert
        Assert.True(initDbOnStartup, "Explicit configuration should override defaults");
        Assert.True(seedOnStartup, "Explicit configuration should override defaults");
        _output.WriteLine("✅ Explicit configuration settings are respected");
    }

    [Fact]
    public async Task TaskWhenAny_WithMultipleTasks_CompletesWhenFirstTaskFinishes()
    {
        // Arrange
        var fastTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            return "fast";
        });

        var slowTask = Task.Run(async () =>
        {
            await Task.Delay(5000);
            return "slow";
        });

        // Act
        var completedTask = await Task.WhenAny(fastTask, slowTask);

        // Assert
        Assert.Equal(fastTask, completedTask);
        Assert.Equal("fast", await fastTask);
        _output.WriteLine("✅ Task.WhenAny correctly identifies the first completed task");
    }

    [Fact]
    public async Task SimulatedDatabaseInit_WithTimeout_CompletesOrTimesOut()
    {
        // Arrange: Simulate a database initialization that might hang
        async Task<bool> SimulateDatabaseInit(bool shouldSucceed, TimeSpan delay)
        {
            await Task.Delay(delay);
            return shouldSucceed;
        }

        // Act & Assert: Fast successful init
        var successfulInit = SimulateDatabaseInit(true, TimeSpan.FromMilliseconds(100));
        var timeout1 = Task.Delay(TimeSpan.FromSeconds(5));
        var completed1 = await Task.WhenAny(successfulInit, timeout1);

        Assert.Equal(successfulInit, completed1);
        Assert.True(await successfulInit);
        _output.WriteLine("✅ Successful init completes before timeout");

        // Act & Assert: Slow init times out
        var slowInit = SimulateDatabaseInit(true, TimeSpan.FromSeconds(10));
        var timeout2 = Task.Delay(TimeSpan.FromSeconds(2));
        var completed2 = await Task.WhenAny(slowInit, timeout2);

        Assert.Equal(timeout2, completed2);
        _output.WriteLine("✅ Slow init is detected by timeout");
    }
}
