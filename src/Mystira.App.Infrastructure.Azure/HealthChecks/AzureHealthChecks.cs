using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mystira.App.Infrastructure.Azure.HealthChecks;

public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly DbContext _context;

    public CosmosDbHealthCheck(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // EF Core Cosmos provider does not support CanConnectAsync.
            // Use the underlying CosmosClient to perform a lightweight connectivity check.
            var cosmosClient = _context.Database.GetCosmosClient();
            await cosmosClient.ReadAccountAsync().ConfigureAwait(false);
            return HealthCheckResult.Healthy("Cosmos DB connection is healthy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Respect cancellation; let the hosting environment decide how to treat this.
            throw;
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
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get blob service properties as a lightweight connectivity check.
            await _blobServiceClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Blob storage connection is healthy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Respect cancellation; higher-layer health infrastructure decides how to interpret this.
            throw;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage connection failed", ex);
        }
    }
}
