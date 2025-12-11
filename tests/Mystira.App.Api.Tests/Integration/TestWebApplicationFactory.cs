using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Azure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mystira.App.Api.Tests.Integration;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Use test-specific configuration
            config.Sources.Clear();
            config.AddJsonFile("appsettings.Testing.json", optional: false);
            
            // Override any additional settings needed for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Add any test-specific overrides here if needed
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database context registration
            services.RemoveAll(typeof(DbContextOptions<MystiraAppDbContext>));
            services.RemoveAll(typeof(MystiraAppDbContext));

            // Add in-memory database for testing with simplified configuration
            services.AddDbContext<MystiraAppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
                // Configure options to be less strict for testing
                options.EnableServiceProviderCaching(false);
                options.EnableSensitiveDataLogging();
            });

            // Replace Azure services with test implementations
            services.RemoveAll<IAzureBlobService>();
            
            // Create a mock Azure Blob Service that always succeeds
            var mockBlobService = new Mock<IAzureBlobService>();
            mockBlobService.Setup(x => x.UploadMediaAsync
                    (It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("test-file-url");
            mockBlobService.Setup(x => x.DeleteMediaAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            mockBlobService.Setup(x => x.GetMediaUrlAsync(It.IsAny<string>()))
                .ReturnsAsync("test-file-url");
            
            services.AddSingleton(mockBlobService.Object);

            // Configure health checks to always pass
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
            });

            // Add test-specific health checks that always pass
            services.AddHealthChecks()
                .AddCheck("database", () => HealthCheckResult.Healthy("Test database is healthy"))
                .AddCheck("storage", () => HealthCheckResult.Healthy("Test storage is healthy"))
                .AddCheck("memory", () => HealthCheckResult.Healthy("Memory usage is normal"));

            // Ensure the database is created for each test with error handling
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MystiraAppDbContext>();
            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the test setup
                Console.WriteLine($"Warning: Database creation failed: {ex.Message}");
                // For tests, we can continue without the database being fully configured
            }
        });

        builder.UseEnvironment("Testing");
    }
}