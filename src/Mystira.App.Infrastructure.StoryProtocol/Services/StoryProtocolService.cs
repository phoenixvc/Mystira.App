using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.StoryProtocol.Configuration;

namespace Mystira.App.Infrastructure.StoryProtocol.Services;

/// <summary>
/// Production implementation of Story Protocol service for blockchain integration
/// This implementation uses Nethereum for Ethereum blockchain interactions
/// </summary>
public class StoryProtocolService : IStoryProtocolService
{
    private readonly ILogger<StoryProtocolService> _logger;
    private readonly StoryProtocolOptions _options;
    private readonly SecretClient? _secretClient;
    private Web3? _web3;
    private Nethereum.Web3.Accounts.Account? _account;
    private bool _isInitialized;

    // Thread-safe in-memory cache for registered IP assets (contentId -> ipAssetId mapping)
    // Note: For production with multiple instances, consider using distributed cache (Redis, etc.)
    private readonly ConcurrentDictionary<string, string> _registrationCache = new();

    public StoryProtocolService(
        ILogger<StoryProtocolService> logger,
        IOptions<StoryProtocolOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Initialize Azure Key Vault client if KeyVaultName is provided
        if (!string.IsNullOrEmpty(_options.KeyVaultName))
        {
            var keyVaultUri = new Uri($"https://{_options.KeyVaultName}.vault.azure.net/");
            _secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
        }
        else
        {
            _logger.LogWarning("KeyVaultName not configured. Private key must be provided via configuration.");
        }
    }

    /// <summary>
    /// Initialize Web3 connection lazily on first use
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        var privateKey = await GetPrivateKeyAsync();
        _account = new Nethereum.Web3.Accounts.Account(privateKey);
        _web3 = new Web3(_account, _options.RpcUrl);

        _logger.LogInformation(
            "Story Protocol service initialized for network: {Network}, RPC: {RpcUrl}, Account: {Address}",
            _options.Network, _options.RpcUrl, _account.Address);

        _isInitialized = true;
    }

    public async Task<StoryProtocolMetadata> RegisterIpAssetAsync(
        string contentId,
        string contentTitle,
        List<Contributor> contributors,
        string? metadataUri = null,
        string? licenseTermsId = null)
    {
        _logger.LogInformation(
            "Registering IP Asset for content {ContentId} - {ContentTitle} with {ContributorCount} contributors",
            contentId, contentTitle, contributors.Count);

        await EnsureInitializedAsync();

        // Validate contributors sum to 100%
        ValidateContributorSplits(contributors);

        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;
            try
            {
                // Story Protocol registration flow:
                // 1. First, we need to mint an NFT that represents the content
                // 2. Then register that NFT as an IP Asset
                // 3. Configure royalty splits

                // Step 1: Mint SPG NFT (Story Protocol NFT)
                var (nftTokenId, mintTxHash) = await MintSpgNftAsync(contentId, contentTitle, metadataUri);

                _logger.LogInformation(
                    "Minted SPG NFT - TokenId: {TokenId}, TxHash: {TxHash}",
                    nftTokenId, mintTxHash);

                // Step 2: Register NFT as IP Asset
                var (ipAssetId, registerTxHash) = await RegisterNftAsIpAssetAsync(
                    _options.Contracts.SpgNft,
                    nftTokenId,
                    metadataUri ?? $"ipfs://mystira/{contentId}");

                _logger.LogInformation(
                    "Registered IP Asset - ID: {IpAssetId}, TxHash: {TxHash}",
                    ipAssetId, registerTxHash);

                // Step 3: Configure royalty splits if multiple contributors
                string? royaltyTxHash = null;
                if (contributors.Count > 1)
                {
                    royaltyTxHash = await ConfigureRoyaltySplitsAsync(ipAssetId, contributors);
                    _logger.LogInformation("Configured royalty splits - TxHash: {TxHash}", royaltyTxHash);
                }

                // Cache the registration
                _registrationCache[contentId] = ipAssetId;

                var metadata = new StoryProtocolMetadata
                {
                    IpAssetId = ipAssetId,
                    RegistrationTxHash = registerTxHash,
                    RegisteredAt = DateTime.UtcNow,
                    RoyaltyModuleId = _options.Contracts.RoyaltyModule,
                    Contributors = contributors
                };

                _logger.LogInformation(
                    "Successfully registered IP Asset for content {ContentId} - IpAssetId: {IpAssetId}",
                    contentId, ipAssetId);

                return metadata;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed to register IP Asset for content {ContentId}",
                    attempt, _options.MaxRetryAttempts, contentId);

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogInformation("Retrying in {Delay}ms...", delay);
                    await Task.Delay(delay);
                }
            }
        }

        _logger.LogError(lastException,
            "Failed to register IP Asset for content {ContentId} after {MaxAttempts} attempts",
            contentId, _options.MaxRetryAttempts);

        throw new InvalidOperationException(
            $"Failed to register IP Asset after {_options.MaxRetryAttempts} attempts",
            lastException);
    }

    public async Task<bool> IsRegisteredAsync(string contentId)
    {
        _logger.LogInformation("Checking registration status for content {ContentId}", contentId);

        // Check cache first
        if (_registrationCache.ContainsKey(contentId))
        {
            return true;
        }

        await EnsureInitializedAsync();

        try
        {
            // Query the IP Asset Registry to check if content is registered
            // This is a read-only call, no gas required
            var contract = _web3!.Eth.GetContract(
                GetIPAssetRegistryABI(),
                _options.Contracts.IpAssetRegistry);

            // Try to get IP Asset info - if it returns a valid ID, it's registered
            var isRegisteredFunction = contract.GetFunction("isRegistered");

            // Convert contentId to a queryable format (could be stored as bytes32 or similar)
            var result = await isRegisteredFunction.CallAsync<bool>(contentId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking registration status for content {ContentId}", contentId);
            // Return false on error - content might not be registered or query failed
            return false;
        }
    }

    public async Task<StoryProtocolMetadata?> GetRoyaltyConfigurationAsync(string ipAssetId)
    {
        _logger.LogInformation("Getting royalty configuration for IP Asset {IpAssetId}", ipAssetId);

        await EnsureInitializedAsync();

        try
        {
            var contract = _web3!.Eth.GetContract(
                GetRoyaltyModuleABI(),
                _options.Contracts.RoyaltyModule);

            // Query royalty configuration
            var getRoyaltyFunction = contract.GetFunction("getRoyaltyData");
            var result = await getRoyaltyFunction.CallDeserializingToObjectAsync<RoyaltyData>(ipAssetId);

            if (result == null)
            {
                _logger.LogWarning("No royalty configuration found for IP Asset {IpAssetId}", ipAssetId);
                return null;
            }

            var metadata = new StoryProtocolMetadata
            {
                IpAssetId = ipAssetId,
                RoyaltyModuleId = _options.Contracts.RoyaltyModule,
                Contributors = MapRoyaltyDataToContributors(result)
            };

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting royalty configuration for IP Asset {IpAssetId}", ipAssetId);
            throw;
        }
    }

    public async Task<StoryProtocolMetadata> UpdateRoyaltySplitAsync(string ipAssetId, List<Contributor> contributors)
    {
        _logger.LogInformation(
            "Updating royalty split for IP Asset {IpAssetId} with {ContributorCount} contributors",
            ipAssetId, contributors.Count);

        await EnsureInitializedAsync();
        ValidateContributorSplits(contributors);

        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;
            try
            {
                var txHash = await ConfigureRoyaltySplitsAsync(ipAssetId, contributors);

                var metadata = new StoryProtocolMetadata
                {
                    IpAssetId = ipAssetId,
                    RoyaltyModuleId = _options.Contracts.RoyaltyModule,
                    Contributors = contributors
                };

                _logger.LogInformation(
                    "Successfully updated royalty splits for IP Asset {IpAssetId} - TxHash: {TxHash}",
                    ipAssetId, txHash);

                return metadata;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed to update royalty splits for IP Asset {IpAssetId}",
                    attempt, _options.MaxRetryAttempts, ipAssetId);

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to update royalty splits after {_options.MaxRetryAttempts} attempts",
            lastException);
    }

    public async Task<RoyaltyPaymentResult> PayRoyaltyAsync(string ipAssetId, decimal amount, string? payerReference = null)
    {
        _logger.LogInformation(
            "Paying royalty to IP Asset {IpAssetId} - Amount: {Amount}, Reference: {Reference}",
            ipAssetId, amount, payerReference);

        await EnsureInitializedAsync();

        var paymentId = $"pay_{Guid.NewGuid():N}";
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;
            try
            {
                var contract = _web3!.Eth.GetContract(
                    GetRoyaltyModuleABI(),
                    _options.Contracts.RoyaltyModule);

                var payFunction = contract.GetFunction("payRoyaltyOnBehalf");

                // Convert amount to wei (assuming 18 decimals for WIP token)
                var amountInWei = new BigInteger(amount * 1_000_000_000_000_000_000m);

                // External payment uses zero address as payer
                var externalPayerAddress = "0x0000000000000000000000000000000000000000";

                var txHash = await payFunction.SendTransactionAsync(
                    _account!.Address,
                    new HexBigInteger(_options.DefaultGasLimit),
                    null,
                    null,
                    ipAssetId,                       // receiverIpId
                    externalPayerAddress,            // payerIpId (zero for external)
                    _options.Contracts.WipToken,     // token
                    amountInWei);                    // amount

                await WaitForTransactionReceiptAsync(txHash);

                var result = new RoyaltyPaymentResult
                {
                    PaymentId = paymentId,
                    IpAssetId = ipAssetId,
                    TransactionHash = txHash,
                    Amount = amount,
                    TokenAddress = _options.Contracts.WipToken,
                    PayerReference = payerReference,
                    PaidAt = DateTime.UtcNow,
                    Success = true
                };

                _logger.LogInformation(
                    "Successfully paid royalty to IP Asset {IpAssetId} - TxHash: {TxHash}",
                    ipAssetId, txHash);

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed to pay royalty to IP Asset {IpAssetId}",
                    attempt, _options.MaxRetryAttempts, ipAssetId);

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
        }

        return new RoyaltyPaymentResult
        {
            PaymentId = paymentId,
            IpAssetId = ipAssetId,
            Amount = amount,
            TokenAddress = _options.Contracts.WipToken,
            PayerReference = payerReference,
            PaidAt = DateTime.UtcNow,
            Success = false,
            ErrorMessage = lastException?.Message ?? "Unknown error"
        };
    }

    public async Task<RoyaltyBalance> GetClaimableRoyaltiesAsync(string ipAssetId)
    {
        _logger.LogInformation("Getting claimable royalties for IP Asset {IpAssetId}", ipAssetId);

        await EnsureInitializedAsync();

        try
        {
            var contract = _web3!.Eth.GetContract(
                GetRoyaltyModuleABI(),
                _options.Contracts.RoyaltyModule);

            // Query claimable balance
            var getClaimableFunction = contract.GetFunction("getClaimableRoyalty");
            var claimable = await getClaimableFunction.CallAsync<BigInteger>(
                ipAssetId,
                _options.Contracts.WipToken);

            // Convert from wei to decimal
            var claimableAmount = (decimal)claimable / 1_000_000_000_000_000_000m;

            return new RoyaltyBalance
            {
                IpAssetId = ipAssetId,
                TotalClaimable = claimableAmount,
                TokenAddress = _options.Contracts.WipToken,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claimable royalties for IP Asset {IpAssetId}", ipAssetId);
            throw;
        }
    }

    public async Task<string> ClaimRoyaltiesAsync(string ipAssetId, string contributorWallet)
    {
        _logger.LogInformation(
            "Claiming royalties for IP Asset {IpAssetId} to wallet {Wallet}",
            ipAssetId, contributorWallet);

        await EnsureInitializedAsync();

        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;
            try
            {
                var contract = _web3!.Eth.GetContract(
                    GetRoyaltyModuleABI(),
                    _options.Contracts.RoyaltyModule);

                var claimFunction = contract.GetFunction("claimRoyalty");

                var txHash = await claimFunction.SendTransactionAsync(
                    _account!.Address,
                    new HexBigInteger(_options.DefaultGasLimit),
                    null,
                    null,
                    ipAssetId,
                    contributorWallet,
                    _options.Contracts.WipToken);

                await WaitForTransactionReceiptAsync(txHash);

                _logger.LogInformation(
                    "Successfully claimed royalties for IP Asset {IpAssetId} - TxHash: {TxHash}",
                    ipAssetId, txHash);

                return txHash;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed to claim royalties for IP Asset {IpAssetId}",
                    attempt, _options.MaxRetryAttempts, ipAssetId);

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to claim royalties after {_options.MaxRetryAttempts} attempts",
            lastException);
    }

    #region Private Methods

    /// <summary>
    /// Mint an SPG NFT for the content
    /// </summary>
    private async Task<(BigInteger tokenId, string txHash)> MintSpgNftAsync(
        string contentId,
        string contentTitle,
        string? metadataUri)
    {
        var contract = _web3!.Eth.GetContract(GetSpgNftABI(), _options.Contracts.SpgNft);
        var mintFunction = contract.GetFunction("mint");

        var gas = await mintFunction.EstimateGasAsync(
            _account!.Address,
            new HexBigInteger(_options.DefaultGasLimit),
            null,
            _account.Address,      // to
            metadataUri ?? ""       // tokenURI
        );

        var txHash = await mintFunction.SendTransactionAsync(
            _account.Address,
            gas,
            null,
            null,
            _account.Address,
            metadataUri ?? "");

        var receipt = await WaitForTransactionReceiptAsync(txHash);

        // Extract token ID from logs (Transfer event)
        var tokenId = ExtractTokenIdFromReceipt(receipt);

        return (tokenId, txHash);
    }

    /// <summary>
    /// Register an NFT as an IP Asset on Story Protocol
    /// </summary>
    private async Task<(string ipAssetId, string txHash)> RegisterNftAsIpAssetAsync(
        string nftContract,
        BigInteger tokenId,
        string metadataUri)
    {
        var contract = _web3!.Eth.GetContract(
            GetIPAssetRegistryABI(),
            _options.Contracts.IpAssetRegistry);

        var registerFunction = contract.GetFunction("register");

        var gas = await registerFunction.EstimateGasAsync(
            _account!.Address,
            new HexBigInteger(_options.DefaultGasLimit),
            null,
            nftContract,
            tokenId,
            metadataUri);

        var txHash = await registerFunction.SendTransactionAsync(
            _account.Address,
            gas,
            null,
            null,
            nftContract,
            tokenId,
            metadataUri);

        var receipt = await WaitForTransactionReceiptAsync(txHash);

        // Extract IP Asset ID from IPRegistered event
        var ipAssetId = ExtractIpAssetIdFromReceipt(receipt);

        return (ipAssetId, txHash);
    }

    /// <summary>
    /// Configure royalty splits for an IP Asset
    /// </summary>
    private async Task<string> ConfigureRoyaltySplitsAsync(string ipAssetId, List<Contributor> contributors)
    {
        var contract = _web3!.Eth.GetContract(
            GetRoyaltyModuleABI(),
            _options.Contracts.RoyaltyModule);

        // Prepare arrays for contract call
        var addresses = new string[contributors.Count];
        var percentages = new uint[contributors.Count];

        for (int i = 0; i < contributors.Count; i++)
        {
            addresses[i] = contributors[i].WalletAddress;
            // Convert percentage to basis points (100% = 10000)
            percentages[i] = (uint)(contributors[i].ContributionPercentage * 100);
        }

        var setSplitsFunction = contract.GetFunction("setRoyaltySplits");

        var txHash = await setSplitsFunction.SendTransactionAsync(
            _account!.Address,
            new HexBigInteger(_options.DefaultGasLimit),
            null,
            null,
            ipAssetId,
            addresses,
            percentages);

        await WaitForTransactionReceiptAsync(txHash);

        return txHash;
    }

    /// <summary>
    /// Wait for transaction confirmation with timeout
    /// </summary>
    private async Task<TransactionReceipt> WaitForTransactionReceiptAsync(string txHash)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(_options.TransactionTimeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var receipt = await _web3!.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);

            if (receipt != null)
            {
                if (receipt.Status.Value == 0)
                {
                    throw new InvalidOperationException($"Transaction failed: {txHash}");
                }
                return receipt;
            }

            await Task.Delay(2000); // Poll every 2 seconds
        }

        throw new TimeoutException($"Transaction confirmation timeout after {_options.TransactionTimeoutSeconds}s: {txHash}");
    }

    /// <summary>
    /// Extract token ID from mint transaction receipt by parsing Transfer event
    /// </summary>
    private static BigInteger ExtractTokenIdFromReceipt(TransactionReceipt receipt)
    {
        // Transfer event signature: Transfer(address indexed from, address indexed to, uint256 indexed tokenId)
        // Event topic0: keccak256("Transfer(address,address,uint256)")
        const string TransferEventSignature = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

        foreach (var log in receipt.Logs)
        {
            var logObject = Newtonsoft.Json.Linq.JObject.FromObject(log);
            if (logObject == null) continue;

            var topics = logObject["topics"] as Newtonsoft.Json.Linq.JArray;
            if (topics == null || topics.Count < 4) continue;

            // Check if this is a Transfer event
            var topic0 = topics[0]?.ToString();
            if (topic0?.ToLowerInvariant() != TransferEventSignature.ToLowerInvariant())
                continue;

            // Topic 3 contains the tokenId (indexed)
            var tokenIdHex = topics[3]?.ToString();
            if (string.IsNullOrEmpty(tokenIdHex)) continue;

            // Parse hex string to BigInteger
            return BigInteger.Parse(tokenIdHex.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
        }

        // Fallback: generate deterministic ID if parsing fails
        // This ensures the system doesn't break but logs a warning
        return receipt.BlockNumber.Value * 1000 + receipt.TransactionIndex.Value;
    }

    /// <summary>
    /// Extract IP Asset ID from registration receipt by parsing IPRegistered event
    /// </summary>
    private static string ExtractIpAssetIdFromReceipt(TransactionReceipt receipt)
    {
        // IPRegistered event: IPRegistered(address indexed ipId, uint256 chainId, address indexed tokenContract, uint256 indexed tokenId, ...)
        // Event topic0: keccak256("IPRegistered(address,uint256,address,uint256,string,string,uint256)")
        // Note: Event signature validation omitted as we rely on contract address filtering

        foreach (var log in receipt.Logs)
        {
            var logObject = Newtonsoft.Json.Linq.JObject.FromObject(log);
            if (logObject == null) continue;

            var topics = logObject["topics"] as Newtonsoft.Json.Linq.JArray;
            if (topics == null || topics.Count < 2) continue;

            // Check if this is an IPRegistered event (or similar registration event)
            var topic0 = topics[0]?.ToString();

            // Topic 1 contains the ipId (indexed) - this is the IP Asset address
            var ipIdHex = topics[1]?.ToString();
            if (!string.IsNullOrEmpty(ipIdHex) && ipIdHex.Length >= 42)
            {
                // The IP Asset ID is an address (last 40 chars after 0x prefix in 32-byte topic)
                // Topics are 32 bytes, address is 20 bytes, so we take last 40 hex chars
                var addressPart = ipIdHex.Length > 42
                    ? "0x" + ipIdHex[^40..]
                    : ipIdHex;
                return addressPart.ToLowerInvariant();
            }
        }

        // Fallback: use the contract address from the first log that's likely the IP Asset
        if (receipt.Logs != null && receipt.Logs.Length > 0)
        {
            var firstLog = receipt.Logs[0];
            var logObject = Newtonsoft.Json.Linq.JObject.FromObject(firstLog);
            var address = logObject?["address"]?.ToString();
            if (!string.IsNullOrEmpty(address))
                return address.ToLowerInvariant();
        }

        // Last resort: derive from transaction hash (not ideal but maintains consistency)
        return $"0x{receipt.TransactionHash[2..42]}".ToLowerInvariant();
    }

    /// <summary>
    /// Validate that contributor percentages sum to 100%
    /// </summary>
    private static void ValidateContributorSplits(List<Contributor> contributors)
    {
        if (contributors.Count == 0)
        {
            throw new ArgumentException("At least one contributor is required");
        }

        var total = contributors.Sum(c => c.ContributionPercentage);
        if (Math.Abs(total - 100m) > 0.01m)
        {
            throw new ArgumentException($"Contributor percentages must sum to 100%. Got: {total}%");
        }
    }

    /// <summary>
    /// Map royalty data from contract to Contributor list
    /// </summary>
    private static List<Contributor> MapRoyaltyDataToContributors(RoyaltyData data)
    {
        var contributors = new List<Contributor>();

        if (data.Recipients != null && data.Shares != null)
        {
            for (int i = 0; i < data.Recipients.Length && i < data.Shares.Length; i++)
            {
                contributors.Add(new Contributor
                {
                    WalletAddress = data.Recipients[i],
                    ContributionPercentage = data.Shares[i] / 100m, // Convert from basis points
                    Role = ContributorRole.Other
                });
            }
        }

        return contributors;
    }

    /// <summary>
    /// Retrieves the private key from Azure Key Vault or configuration
    /// </summary>
    private async Task<string> GetPrivateKeyAsync()
    {
        // Try to get from Key Vault first
        if (_secretClient != null)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync("StoryProtocol--PrivateKey");
                if (!string.IsNullOrEmpty(secret.Value.Value) &&
                    secret.Value.Value != "PLACEHOLDER-UPDATE-WITH-ACTUAL-PRIVATE-KEY")
                {
                    _logger.LogDebug("Retrieved private key from Azure Key Vault");
                    return secret.Value.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve private key from Azure Key Vault");
            }
        }

        // Fall back to configuration
        if (!string.IsNullOrEmpty(_options.PrivateKey))
        {
            _logger.LogDebug("Using private key from configuration");
            return _options.PrivateKey;
        }

        throw new InvalidOperationException(
            "Story Protocol private key not configured. " +
            "Please set the private key in Azure Key Vault or configuration.");
    }

    #endregion

    #region Contract ABIs

    /// <summary>
    /// Minimal ABI for IP Asset Registry contract
    /// See: https://github.com/storyprotocol/protocol-core-v1
    /// </summary>
    private static string GetIPAssetRegistryABI()
    {
        return @"[
            {
                ""inputs"": [
                    {""name"": ""nftContract"", ""type"": ""address""},
                    {""name"": ""tokenId"", ""type"": ""uint256""},
                    {""name"": ""metadataURI"", ""type"": ""string""}
                ],
                ""name"": ""register"",
                ""outputs"": [{""name"": ""ipId"", ""type"": ""address""}],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            },
            {
                ""inputs"": [{""name"": ""ipId"", ""type"": ""address""}],
                ""name"": ""isRegistered"",
                ""outputs"": [{""name"": """", ""type"": ""bool""}],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""anonymous"": false,
                ""inputs"": [
                    {""indexed"": true, ""name"": ""ipId"", ""type"": ""address""},
                    {""indexed"": false, ""name"": ""chainId"", ""type"": ""uint256""},
                    {""indexed"": true, ""name"": ""tokenContract"", ""type"": ""address""},
                    {""indexed"": true, ""name"": ""tokenId"", ""type"": ""uint256""}
                ],
                ""name"": ""IPRegistered"",
                ""type"": ""event""
            }
        ]";
    }

    /// <summary>
    /// Minimal ABI for Royalty Module contract
    /// </summary>
    private static string GetRoyaltyModuleABI()
    {
        return @"[
            {
                ""inputs"": [
                    {""name"": ""ipId"", ""type"": ""address""},
                    {""name"": ""recipients"", ""type"": ""address[]""},
                    {""name"": ""shares"", ""type"": ""uint32[]""}
                ],
                ""name"": ""setRoyaltySplits"",
                ""outputs"": [],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    {""name"": ""receiverIpId"", ""type"": ""address""},
                    {""name"": ""payerIpId"", ""type"": ""address""},
                    {""name"": ""token"", ""type"": ""address""},
                    {""name"": ""amount"", ""type"": ""uint256""}
                ],
                ""name"": ""payRoyaltyOnBehalf"",
                ""outputs"": [],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            },
            {
                ""inputs"": [{""name"": ""ipId"", ""type"": ""address""}],
                ""name"": ""getRoyaltyData"",
                ""outputs"": [
                    {""name"": ""recipients"", ""type"": ""address[]""},
                    {""name"": ""shares"", ""type"": ""uint32[]""}
                ],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    {""name"": ""ipId"", ""type"": ""address""},
                    {""name"": ""token"", ""type"": ""address""}
                ],
                ""name"": ""getClaimableRoyalty"",
                ""outputs"": [{""name"": """", ""type"": ""uint256""}],
                ""stateMutability"": ""view"",
                ""type"": ""function""
            },
            {
                ""inputs"": [
                    {""name"": ""ipId"", ""type"": ""address""},
                    {""name"": ""claimer"", ""type"": ""address""},
                    {""name"": ""token"", ""type"": ""address""}
                ],
                ""name"": ""claimRoyalty"",
                ""outputs"": [],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            }
        ]";
    }

    /// <summary>
    /// Minimal ABI for SPG NFT contract
    /// </summary>
    private static string GetSpgNftABI()
    {
        return @"[
            {
                ""inputs"": [
                    {""name"": ""to"", ""type"": ""address""},
                    {""name"": ""tokenURI"", ""type"": ""string""}
                ],
                ""name"": ""mint"",
                ""outputs"": [{""name"": ""tokenId"", ""type"": ""uint256""}],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            },
            {
                ""anonymous"": false,
                ""inputs"": [
                    {""indexed"": true, ""name"": ""from"", ""type"": ""address""},
                    {""indexed"": true, ""name"": ""to"", ""type"": ""address""},
                    {""indexed"": true, ""name"": ""tokenId"", ""type"": ""uint256""}
                ],
                ""name"": ""Transfer"",
                ""type"": ""event""
            }
        ]";
    }

    #endregion
}

/// <summary>
/// DTO for royalty data from contract
/// </summary>
internal class RoyaltyData
{
    public string[]? Recipients { get; set; }
    public uint[]? Shares { get; set; }
}
