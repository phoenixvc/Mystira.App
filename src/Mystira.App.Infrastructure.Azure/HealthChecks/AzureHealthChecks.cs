using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mystira.App.Infrastructure.Azure.HealthChecks;

public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly DbContext _context;

    public CosmosDbHealthCheck(DbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to connect to the database
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Cosmos DB connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cosmos DB connection failed", ex);
        }
    }
}

public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageHealthCheck(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get blob service properties
            await _blobServiceClient.GetPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy("Blob storage connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage connection failed", ex);
        }
    }
}