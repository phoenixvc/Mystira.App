using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;
using Xunit;

namespace DMfinity.Api.Tests.Integration;

/// <summary>
/// Integration tests that verify the system operates in a healthy state
/// with all dependencies properly mocked or configured for testing
/// </summary>
public class HealthySystemIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthySystemIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthChecks_AllDependencies_ShouldBeHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Health endpoint should return OK when system is healthy");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Verify overall status is healthy
        healthResponse.TryGetProperty("status", out var statusElement).Should().BeTrue();
        statusElement.GetString().Should().Be("Healthy");
        
        // Verify duration is present
        healthResponse.TryGetProperty("duration", out var durationElement).Should().BeTrue();
        
        // Verify individual health checks
        healthResponse.TryGetProperty("results", out var resultsElement).Should().BeTrue();
        var results = resultsElement.EnumerateObject().ToDictionary(
            prop => prop.Name,
            prop => prop.Value
        );
        
        // Test environment should have our configured health checks
        results.Should().ContainKey("database");
        results.Should().ContainKey("storage");
        results.Should().ContainKey("memory");
    }

    [Fact]
    public async Task HealthCheck_Service_ShouldBeAvailable()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        
        // Act
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
        var result = await healthCheckService.CheckHealthAsync();
        
        // Assert
        result.Status.Should().Be(HealthStatus.Healthy, "All configured health checks should pass");
        result.Entries.Should().NotBeEmpty("Health checks should be registered");
        
        // Verify individual checks
        result.Entries.Should().ContainKey("database");
        result.Entries.Should().ContainKey("storage");
        result.Entries.Should().ContainKey("memory");
        
        foreach (var (name, entry) in result.Entries)
        {
            entry.Status.Should().Be(HealthStatus.Healthy, $"Health check '{name}' should be healthy");
        }
    }

    [Fact]
    public async Task Application_ShouldStart_WithoutErrors()
    {
        // This test verifies that the application can start successfully
        // with all our test configurations and mocked dependencies
        
        // Act - The fact that we can create a client means the app started successfully
        var response = await _client.GetAsync("/api/health");
        
        // Assert
        response.Should().NotBeNull("Application should start and respond to requests");
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, 
            "Application should not have startup errors");
    }

    [Fact]
    public async Task Database_ShouldBeConfigured_ForTesting()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        
        // Act & Assert - Verify we can get the database context without errors
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.DMfinityDbContext>();
        dbContext.Should().NotBeNull("Database context should be available");
        
        // Verify it's using in-memory database for testing
        var databaseName = dbContext.Database.ProviderName;
        databaseName.Should().Contain("InMemory", "Should be using in-memory database for tests");
    }

    [Theory]
    [InlineData("/api/health")]
    public async Task CriticalEndpoints_ShouldBeAccessible(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, 
            $"Endpoint {endpoint} should not return server error");
        response.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable,
            $"Endpoint {endpoint} should be available in test environment");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturn_ConsistentResults()
    {
        // Arrange - Call the endpoint multiple times
        var responses = new List<HttpResponseMessage>();
        
        // Act
        for (int i = 0; i < 3; i++)
        {
            responses.Add(await _client.GetAsync("/api/health"));
        }
        
        // Assert - All calls should return the same healthy status
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
            healthResponse.TryGetProperty("status", out var statusElement).Should().BeTrue();
            statusElement.GetString().Should().Be("Healthy");
        }
        
        // Clean up
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}