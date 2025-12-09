using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.StoryProtocol.Configuration;

namespace Mystira.App.Infrastructure.StoryProtocol.Services;

/// <summary>
/// Mock implementation of Story Protocol service for development/testing
/// This implementation simulates blockchain interactions without actual on-chain transactions
/// </summary>
public class MockStoryProtocolService : IStoryProtocolService
{
    private readonly ILogger<MockStoryProtocolService> _logger;
    private readonly StoryProtocolOptions _options;
    private readonly ConcurrentDictionary<string, StoryProtocolMetadata> _registeredAssets = new();

    public MockStoryProtocolService(
        ILogger<MockStoryProtocolService> logger,
        IOptions<StoryProtocolOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<StoryProtocolMetadata> RegisterIpAssetAsync(
        string contentId,
        string contentTitle,
        List<Contributor> contributors,
        string? metadataUri = null,
        string? licenseTermsId = null)
    {
        _logger.LogInformation(
            "Mock: Registering IP Asset for content {ContentId} - {ContentTitle} with {ContributorCount} contributors",
            contentId, contentTitle, contributors.Count);

        // Generate mock IDs
        var ipAssetId = $"ip-asset-{Guid.NewGuid():N}";
        var txHash = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}";
        var royaltyModuleId = $"royalty-{Guid.NewGuid():N}";

        var metadata = new StoryProtocolMetadata
        {
            IpAssetId = ipAssetId,
            RegistrationTxHash = txHash,
            RegisteredAt = DateTime.UtcNow,
            RoyaltyModuleId = royaltyModuleId,
            Contributors = contributors
        };

        // Store in memory for later retrieval
        _registeredAssets[contentId] = metadata;
        _registeredAssets[ipAssetId] = metadata;

        _logger.LogInformation(
            "Mock: Registered IP Asset {IpAssetId} for content {ContentId}",
            ipAssetId, contentId);

        return Task.FromResult(metadata);
    }

    public Task<bool> IsRegisteredAsync(string contentId)
    {
        var isRegistered = _registeredAssets.ContainsKey(contentId);
        _logger.LogInformation(
            "Mock: Checking registration status for {ContentId}: {IsRegistered}",
            contentId, isRegistered);
        return Task.FromResult(isRegistered);
    }

    public Task<StoryProtocolMetadata?> GetRoyaltyConfigurationAsync(string ipAssetId)
    {
        _logger.LogInformation(
            "Mock: Getting royalty configuration for IP Asset {IpAssetId}",
            ipAssetId);

        _registeredAssets.TryGetValue(ipAssetId, out var metadata);
        return Task.FromResult(metadata);
    }

    public Task<StoryProtocolMetadata> UpdateRoyaltySplitAsync(string ipAssetId, List<Contributor> contributors)
    {
        _logger.LogInformation(
            "Mock: Updating royalty split for IP Asset {IpAssetId} with {ContributorCount} contributors",
            ipAssetId, contributors.Count);

        if (!_registeredAssets.TryGetValue(ipAssetId, out var metadata))
        {
            throw new ArgumentException($"IP Asset not found: {ipAssetId}");
        }

        // Update contributors
        metadata.Contributors = contributors;

        _logger.LogInformation(
            "Mock: Updated royalty split for IP Asset {IpAssetId}",
            ipAssetId);

        return Task.FromResult(metadata);
    }

    public Task<RoyaltyPaymentResult> PayRoyaltyAsync(string ipAssetId, decimal amount, string? payerReference = null)
    {
        _logger.LogInformation(
            "Mock: Processing royalty payment of {Amount} for IP Asset {IpAssetId}",
            amount, ipAssetId);

        if (!_registeredAssets.TryGetValue(ipAssetId, out var metadata))
        {
            return Task.FromResult(new RoyaltyPaymentResult
            {
                Success = false,
                IpAssetId = ipAssetId,
                TotalAmount = amount,
                ErrorMessage = $"IP Asset not found: {ipAssetId}"
            });
        }

        // Create mock distribution based on contributors
        var distributions = new List<RoyaltyDistribution>();
        foreach (var contributor in metadata.Contributors)
        {
            distributions.Add(new RoyaltyDistribution
            {
                ContributorId = contributor.Id,
                ContributorName = contributor.Name,
                WalletAddress = contributor.WalletAddress,
                SharePercentage = contributor.ContributionPercentage,
                Amount = amount * (contributor.ContributionPercentage / 100m)
            });
        }

        var result = new RoyaltyPaymentResult
        {
            Success = true,
            IpAssetId = ipAssetId,
            TotalAmount = amount,
            TransactionHash = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}",
            Distributions = distributions,
            ProcessedAt = DateTime.UtcNow,
            PayerReference = payerReference
        };

        _logger.LogInformation(
            "Mock: Royalty payment processed successfully. TxHash: {TxHash}",
            result.TransactionHash);

        return Task.FromResult(result);
    }

    public Task<RoyaltyBalance> GetClaimableRoyaltiesAsync(string ipAssetId)
    {
        _logger.LogInformation(
            "Mock: Getting claimable royalties for IP Asset {IpAssetId}",
            ipAssetId);

        if (!_registeredAssets.TryGetValue(ipAssetId, out var metadata))
        {
            return Task.FromResult(new RoyaltyBalance
            {
                IpAssetId = ipAssetId,
                TotalClaimable = 0,
                ContributorBalances = new List<ContributorBalance>()
            });
        }

        // Generate mock balances for each contributor
        var random = new Random();
        var contributorBalances = new List<ContributorBalance>();
        decimal totalClaimable = 0;

        foreach (var contributor in metadata.Contributors)
        {
            // Mock some random claimable amount
            var balance = Math.Round((decimal)(random.NextDouble() * 10), 6);
            totalClaimable += balance;

            contributorBalances.Add(new ContributorBalance
            {
                ContributorId = contributor.Id,
                ContributorName = contributor.Name,
                WalletAddress = contributor.WalletAddress,
                SharePercentage = contributor.ContributionPercentage,
                ClaimableAmount = balance,
                TotalEarned = balance * 2, // Mock that they've earned double and claimed half
                TotalClaimed = balance
            });
        }

        return Task.FromResult(new RoyaltyBalance
        {
            IpAssetId = ipAssetId,
            TotalClaimable = totalClaimable,
            ContributorBalances = contributorBalances,
            LastUpdated = DateTime.UtcNow
        });
    }

    public Task<string> ClaimRoyaltiesAsync(string ipAssetId, string contributorWallet)
    {
        _logger.LogInformation(
            "Mock: Claiming royalties for wallet {Wallet} from IP Asset {IpAssetId}",
            contributorWallet, ipAssetId);

        if (!_registeredAssets.TryGetValue(ipAssetId, out _))
        {
            throw new ArgumentException($"IP Asset not found: {ipAssetId}");
        }

        // Generate mock transaction hash
        var txHash = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Mock: Royalties claimed successfully. TxHash: {TxHash}",
            txHash);

        return Task.FromResult(txHash);
    }
}
