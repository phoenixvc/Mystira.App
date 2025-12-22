using System.Numerics;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Configuration.StoryProtocol;
using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;
using Mystira.Chain.V1;
using ProtoContributor = Mystira.Chain.V1.Contributor;

// Alias for proto Contributor type to avoid ambiguity with domain Contributor
using ProtoContributor = Mystira.Chain.V1.Contributor;

namespace Mystira.App.Infrastructure.StoryProtocol.Services;

/// <summary>
/// gRPC-based implementation of IStoryProtocolService that communicates with Mystira.Chain.
/// This adapter translates between the domain interface and the gRPC service contract.
/// </summary>
/// <remarks>
/// This implementation replaces direct blockchain calls (Nethereum) with gRPC calls to
/// Mystira.Chain (Python), providing better separation of concerns and performance.
/// See ADR-0013 for architectural rationale.
/// </remarks>
public class GrpcChainServiceAdapter : IStoryProtocolService, IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly ChainService.ChainServiceClient _client;
    private readonly ILogger<GrpcChainServiceAdapter> _logger;
    private readonly ChainServiceOptions _options;
    private readonly Metadata? _authMetadata;

    public GrpcChainServiceAdapter(
        IOptions<ChainServiceOptions> options,
        ILogger<GrpcChainServiceAdapter> logger)
    {
        _logger = logger;
        _options = options.Value;

        // Validate GrpcEndpoint is configured
        if (string.IsNullOrWhiteSpace(_options.GrpcEndpoint))
        {
            throw new InvalidOperationException(
                "GrpcEndpoint is not configured. Ensure ChainService:GrpcEndpoint is set in configuration " +
                "(typically loaded from Azure Key Vault as ChainService--GrpcEndpoint). " +
                "See ADR-0013 for gRPC endpoint configuration requirements.");
        }

        _logger.LogInformation(
            "Initializing gRPC Chain Service adapter for endpoint: {Endpoint}",
            _options.GrpcEndpoint);

        var channelOptions = new GrpcChannelOptions
        {
            // Configure retry policy
            // Note: RetryPolicy configuration may require additional setup in gRPC 2.71+
            // ServiceConfig = new ServiceConfig
            // {
            //     MethodConfigs =
            //     {
            //         new MethodConfig
            //         {
            //             Names = { MethodName.Default },
            //             RetryPolicy = new RetryPolicy
            //             {
            //                 MaxAttempts = _options.MaxRetryAttempts,
            //                 InitialBackoff = TimeSpan.FromMilliseconds(_options.RetryBaseDelayMs),
            //                 MaxBackoff = TimeSpan.FromSeconds(30),
            //                 BackoffMultiplier = 2,
            //                 RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded }
            //             }
            //         }
            //     }
            // }
        };

        _channel = GrpcChannel.ForAddress(_options.GrpcEndpoint, channelOptions);
        _client = new ChainService.ChainServiceClient(_channel);

        // Set up authentication metadata if API key is configured
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _authMetadata = new Metadata
            {
                { _options.ApiKeyHeaderName, _options.ApiKey }
            };
            _logger.LogDebug("API key authentication configured for Chain service");
        }
        else
        {
            _authMetadata = null;
            _logger.LogDebug("No API key configured for Chain service");
        }
    }

    /// <summary>
    /// Creates call options with authentication and deadline
    /// </summary>
    private CallOptions CreateCallOptions(int? timeoutSeconds = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds ?? _options.TimeoutSeconds);
        return new CallOptions(headers: _authMetadata, deadline: deadline);
    }

    /// <inheritdoc />
    public async Task<StoryProtocolMetadata> RegisterIpAssetAsync(
        string contentId,
        string contentTitle,
        List<Mystira.App.Domain.Models.Contributor> contributors,
        string? metadataUri = null,
        string? licenseTermsId = null)
    {
        _logger.LogInformation(
            "Registering IP Asset via gRPC for content {ContentId} - {ContentTitle} with {ContributorCount} contributors",
            contentId, contentTitle, contributors.Count);

        var request = new RegisterIpAssetRequest
        {
            ContentId = contentId,
            ContentTitle = contentTitle,
            MetadataUri = metadataUri ?? "",
            LicenseTermsId = licenseTermsId ?? "",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Map domain contributors to proto contributors
        foreach (var contributor in contributors)
        {
            request.Contributors.Add(new ProtoContributor
            {
                WalletAddress = contributor.WalletAddress,
                ContributorType = MapContributorType(contributor.Role),
                ShareBasisPoints = (uint)(contributor.ContributionPercentage * 100),
                DisplayName = contributor.Name ?? ""
            });
        }

        try
        {
            var response = await _client.RegisterIpAssetAsync(
                request,
                CreateCallOptions());

            if (response.Status == IpAssetStatus.Failed)
            {
                _logger.LogError(
                    "Failed to register IP Asset for content {ContentId}: {Error}",
                    contentId, response.ErrorMessage);
                throw new InvalidOperationException(
                    $"Failed to register IP Asset: {response.ErrorMessage}");
            }

            var metadata = new StoryProtocolMetadata
            {
                IpAssetId = response.IpAssetId,
                RegistrationTxHash = response.RegistrationTxHash,
                RegisteredAt = response.RegisteredAt?.ToDateTime() ?? DateTime.UtcNow,
                Contributors = contributors
            };

            _logger.LogInformation(
                "Successfully registered IP Asset for content {ContentId} - IpAssetId: {IpAssetId}",
                contentId, response.IpAssetId);

            return metadata;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC error registering IP Asset for content {ContentId}: {Status}",
                contentId, ex.StatusCode);
            throw new InvalidOperationException(
                $"Failed to register IP Asset via Chain service: {ex.Status.Detail}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsRegisteredAsync(string contentId)
    {
        _logger.LogInformation("Checking registration status for content {ContentId} via gRPC", contentId);

        try
        {
            var response = await _client.IsRegisteredAsync(
                new IsRegisteredRequest { ContentId = contentId },
                CreateCallOptions());

            return response.IsRegistered;
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex,
                "gRPC error checking registration for content {ContentId}: {Status}",
                contentId, ex.StatusCode);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<StoryProtocolMetadata?> GetRoyaltyConfigurationAsync(string ipAssetId)
    {
        _logger.LogInformation("Getting royalty configuration for IP Asset {IpAssetId} via gRPC", ipAssetId);

        try
        {
            var response = await _client.GetRoyaltyConfigurationAsync(
                new GetRoyaltyConfigurationRequest { IpAssetId = ipAssetId },
                CreateCallOptions());

            var metadata = new StoryProtocolMetadata
            {
                IpAssetId = ipAssetId,
                RoyaltyModuleId = response.RoyaltyModuleId,
                Contributors = response.Recipients.Select(r => new Mystira.App.Domain.Models.Contributor
                {
                    WalletAddress = r.WalletAddress,
                    ContributionPercentage = r.ShareBasisPoints / 100m,
                    Role = ParseContributorRole(r.Role)
                }).ToList()
            };

            return metadata;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("No royalty configuration found for IP Asset {IpAssetId}", ipAssetId);
            return null;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC error getting royalty configuration for IP Asset {IpAssetId}: {Status}",
                ipAssetId, ex.StatusCode);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<StoryProtocolMetadata> UpdateRoyaltySplitAsync(
        string ipAssetId,
        List<Mystira.App.Domain.Models.Contributor> contributors)
    {
        _logger.LogInformation(
            "Updating royalty split for IP Asset {IpAssetId} with {ContributorCount} contributors via gRPC",
            ipAssetId, contributors.Count);

        var request = new UpdateRoyaltySplitRequest
        {
            IpAssetId = ipAssetId,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        foreach (var contributor in contributors)
        {
            request.Contributors.Add(new ProtoContributor
            {
                WalletAddress = contributor.WalletAddress,
                ContributorType = MapContributorType(contributor.Role),
                ShareBasisPoints = (uint)(contributor.ContributionPercentage * 100)
            });
        }

        try
        {
            var response = await _client.UpdateRoyaltySplitAsync(
                request,
                CreateCallOptions());

            if (!response.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to update royalty splits: {response.ErrorMessage}");
            }

            return new StoryProtocolMetadata
            {
                IpAssetId = ipAssetId,
                Contributors = contributors
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC error updating royalty splits for IP Asset {IpAssetId}: {Status}",
                ipAssetId, ex.StatusCode);
            throw new InvalidOperationException(
                $"Failed to update royalty splits via Chain service: {ex.Status.Detail}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<RoyaltyPaymentResult> PayRoyaltyAsync(
        string ipAssetId,
        decimal amount,
        string? payerReference = null)
    {
        _logger.LogInformation(
            "Paying royalty to IP Asset {IpAssetId} - Amount: {Amount} via gRPC",
            ipAssetId, amount);

        // Convert amount to wei (18 decimals) using BigInteger to avoid overflow
        // long.MaxValue is ~9.2 ETH, so we use BigInteger for amounts > 9.2 ETH
        var amountDecimal = amount * 1_000_000_000_000_000_000m;
        var amountWei = ((BigInteger)amountDecimal).ToString();

        var request = new PayRoyaltiesRequest
        {
            IpAssetId = ipAssetId,
            AmountWei = amountWei,
            CurrencyToken = _options.WipTokenAddress,
            PayerReference = payerReference ?? "",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        try
        {
            var response = await _client.PayRoyaltiesAsync(
                request,
                CreateCallOptions());

            return new RoyaltyPaymentResult
            {
                PaymentId = response.PaymentId ?? Guid.NewGuid().ToString(),
                IpAssetId = ipAssetId,
                TransactionHash = response.TransactionHash,
                Amount = amount,
                TokenAddress = response.CurrencyToken,
                PayerReference = payerReference,
                PaidAt = response.PaidAt?.ToDateTime() ?? DateTime.UtcNow,
                Success = response.Status == PaymentStatus.Confirmed || response.Status == PaymentStatus.Pending,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC error paying royalty to IP Asset {IpAssetId}: {Status}",
                ipAssetId, ex.StatusCode);

            return new RoyaltyPaymentResult
            {
                PaymentId = Guid.NewGuid().ToString(),
                IpAssetId = ipAssetId,
                Amount = amount,
                TokenAddress = _options.WipTokenAddress,
                PayerReference = payerReference,
                PaidAt = DateTime.UtcNow,
                Success = false,
                ErrorMessage = ex.Status.Detail
            };
        }
    }

    /// <inheritdoc />
    public async Task<RoyaltyBalance> GetClaimableRoyaltiesAsync(string ipAssetId)
    {
        _logger.LogInformation("Getting claimable royalties for IP Asset {IpAssetId} via gRPC", ipAssetId);

        try
        {
            var response = await _client.GetClaimableRoyaltiesAsync(
                new GetClaimableRoyaltiesRequest { IpAssetId = ipAssetId },
                CreateCallOptions());

            // Sum up all claimable balances for this IP Asset
            var totalClaimable = response.Balances
                .Where(b => b.IpAssetId == ipAssetId)
                .Sum(b => ParseWeiToDecimal(b.AmountWei));

            return new RoyaltyBalance
            {
                IpAssetId = ipAssetId,
                TotalClaimable = totalClaimable,
                TokenAddress = _options.WipTokenAddress,
                LastUpdated = response.LastUpdated?.ToDateTime() ?? DateTime.UtcNow
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC error getting claimable royalties for IP Asset {IpAssetId}: {Status}",
                ipAssetId, ex.StatusCode);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> ClaimRoyaltiesAsync(string ipAssetId, string contributorWallet)
    {
        _logger.LogInformation(
            "Claiming royalties for IP Asset {IpAssetId} to wallet {Wallet} via gRPC",
            ipAssetId, contributorWallet);

        var request = new ClaimRoyaltiesRequest
        {
            IpAssetId = ipAssetId,
            ContributorWallet = contributorWallet,
            CurrencyToken = _options.WipTokenAddress,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        try
        {
            var response = await _client.ClaimRoyaltiesAsync(
                request,
                CreateCallOptions());

            if (response.Status == PaymentStatus.Failed)
            {
                throw new InvalidOperationException(
                    $"Failed to claim royalties: {response.ErrorMessage}");
            }

            _logger.LogInformation(
                "Successfully claimed royalties for IP Asset {IpAssetId} - TxHash: {TxHash}",
                ipAssetId, response.TransactionHash);

            return response.TransactionHash ?? throw new InvalidOperationException("No transaction hash returned");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex,
                "gRPC error claiming royalties for IP Asset {IpAssetId}: {Status}",
                ipAssetId, ex.StatusCode);
            throw new InvalidOperationException(
                $"Failed to claim royalties via Chain service: {ex.Status.Detail}", ex);
        }
    }

    /// <summary>
    /// Stream transaction status updates (for real-time monitoring)
    /// </summary>
    public async IAsyncEnumerable<TransactionStatusUpdate> WatchTransactionsAsync(
        IEnumerable<string> transactionHashes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new WatchTransactionsRequest();
        request.TransactionHashes.AddRange(transactionHashes);

        using var stream = _client.WatchTransactions(
            request,
            cancellationToken: cancellationToken);

        await foreach (var update in stream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return new TransactionStatusUpdate
            {
                TransactionHash = update.TransactionHash,
                Status = MapTransactionStatus(update.Status),
                Confirmations = (int)update.Confirmations,
                ErrorMessage = update.ErrorMessage,
                Timestamp = update.Timestamp?.ToDateTime() ?? DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Perform health check on the Chain service
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _client.HealthCheckAsync(
                new Google.Protobuf.WellKnownTypes.Empty(),
                CreateCallOptions(timeoutSeconds: 5));

            return response.Status == HealthCheckResponse.Types.ServingStatus.Serving;
        }
        catch (RpcException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.ShutdownAsync();
        GC.SuppressFinalize(this);
    }

    #region Private Helper Methods

    private static ContributorType MapContributorType(ContributorRole role)
    {
        return role switch
        {
            ContributorRole.Writer => ContributorType.Author,
            ContributorRole.Artist => ContributorType.Artist,
            ContributorRole.Editor => ContributorType.Curator,
            ContributorRole.GameDesigner => ContributorType.Publisher,
            _ => ContributorType.Other
        };
    }

    private static ContributorRole ParseContributorRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "author" or "writer" => ContributorRole.Writer,
            "artist" => ContributorRole.Artist,
            "curator" or "editor" => ContributorRole.Editor,
            "publisher" or "gamedesigner" => ContributorRole.GameDesigner,
            _ => ContributorRole.Other
        };
    }

    private static Domain.Models.TransactionStatus MapTransactionStatus(Chain.V1.TransactionStatus status)
    {
        return status switch
        {
            Chain.V1.TransactionStatus.Pending => Domain.Models.TransactionStatus.Pending,
            Chain.V1.TransactionStatus.Confirmed => Domain.Models.TransactionStatus.Confirmed,
            Chain.V1.TransactionStatus.Failed => Domain.Models.TransactionStatus.Failed,
            _ => Domain.Models.TransactionStatus.Unknown
        };
    }

    private static decimal ParseWeiToDecimal(string weiString)
    {
        if (decimal.TryParse(weiString, out var wei))
        {
            return wei / 1_000_000_000_000_000_000m;
        }
        return 0m;
    }

    #endregion
}

/// <summary>
/// Transaction status update for streaming
/// </summary>
public record TransactionStatusUpdate
{
    public required string TransactionHash { get; init; }
    public required Domain.Models.TransactionStatus Status { get; init; }
    public int Confirmations { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; }
}
