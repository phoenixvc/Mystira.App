using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Infrastructure.Azure.Configuration;
using Mystira.App.Infrastructure.Azure.HealthChecks;
using Mystira.App.Infrastructure.Azure.Services;

namespace Mystira.App.Infrastructure.Azure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Azure infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddAzureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Azure options
        services.Configure<AzureOptions>(_ => configuration.GetSection(AzureOptions.SectionName));
        var azureOptions = new AzureOptions();
        configuration.GetSection(AzureOptions.SectionName).Bind(azureOptions);

        // Add Cosmos DB
        services.AddCosmosDb(configuration, azureOptions.CosmosDb);

        // Add Blob Storage
        services.AddBlobStorage(configuration, azureOptions.BlobStorage);

        // Add Health Checks
        services.AddAzureHealthChecks();

        return services;
    }

    /// <summary>
    /// Add Cosmos DB services
    /// </summary>
    public static IServiceCollection AddCosmosDb<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        var cosmosConnectionString = configuration.GetConnectionString("CosmosDb");
        
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            services.AddDbContext<TContext>(options =>
                options.UseCosmos(cosmosConnectionString, "MystiraAppDb"));
        }
        else
        {
            // Fallback to in-memory database for development
            services.AddDbContext<TContext>(options =>
                options.UseInMemoryDatabase("MystiraAppInMemoryDb"));
        }

        return services;
    }

    /// <summary>
    /// Add Cosmos DB services (internal method for infrastructure setup)
    /// </summary>
    private static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration, CosmosDbOptions options)
    {
        var cosmosConnectionString = configuration.GetConnectionString("CosmosDb");
        
        if (!string.IsNullOrEmpty(cosmosConnectionString) && !options.UseInMemoryDatabase)
        {
            // Cosmos DB will be configured by the specific DbContext in the API project
            // This method just sets up the configuration
        }

        return services;
    }

    /// <summary>
    /// Add Azure Blob Storage services
    /// </summary>
    private static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration, BlobStorageOptions options)
    {
        var blobConnectionString = configuration.GetConnectionString("AzureStorage");
        
        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(blobConnectionString));
        }
        else
        {
            // For development, use default Azure credentials
            services.AddSingleton(provider =>
            {
                var accountName = configuration["Azure:Storage:AccountName"] ?? "mystiraappdev";
                return new BlobServiceClient(
                    new Uri($"https://{accountName}.blob.core.windows.net"),
                    new DefaultAzureCredential());
            });
        }

        services.AddScoped<IAzureBlobService, AzureBlobService>();

        return services;
    }

    /// <summary>
    /// Add Azure health checks
    /// </summary>
    private static IServiceCollection AddAzureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<BlobStorageHealthCheck>("blob_storage")
            .AddCheck<CosmosDbHealthCheck>("cosmos_db");

        return services;
    }

    /// <summary>
    /// Add Azure Blob Storage services with explicit configuration
    /// </summary>
    public static IServiceCollection AddAzureBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetConnectionString("AzureStorage");
        
        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(blobConnectionString));
        }
        else
        {
            // For development, use default Azure credentials
            services.AddSingleton(provider =>
            {
                var accountName = configuration["Azure:Storage:AccountName"] ?? "mystiraappdev";
                return new BlobServiceClient(
                    new Uri($"https://{accountName}.blob.core.windows.net"),
                    new DefaultAzureCredential());
            });
        }

        services.AddScoped<IAzureBlobService, AzureBlobService>();

        return services;
    }
}